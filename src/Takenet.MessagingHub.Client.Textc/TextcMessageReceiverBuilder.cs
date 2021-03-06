﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol;
using Takenet.MessagingHub.Client.Connection;
using Takenet.MessagingHub.Client.Listener;
using Takenet.MessagingHub.Client.Messages;
using Takenet.Textc;
using Takenet.Textc.Csdl;
using Takenet.Textc.Processors;
using Takenet.Textc.Scorers;
using Takenet.MessagingHub.Client.Sender;

namespace Takenet.MessagingHub.Client.Textc
{
    /// <summary>
    /// Builder for instances of <see cref="TextcMessageReceiver"/> class. 
    /// </summary>
    public sealed class TextcMessageReceiverBuilder
    {
        //private readonly MessagingHubClientBuilder _clientBuilder;
        private IContextProvider _contextProvider;
        private Func<Message, IMessageReceiver, Task> _matchNotFoundHandler;

        private IOutputProcessor _outputProcessor;
        private ISyntaxParser _syntaxParser;
        private IExpressionScorer _expressionScorer;
        private ICultureProvider _cultureProvider;
        private readonly List<Func<IOutputProcessor, ICommandProcessor>> _commandProcessorFactories;
        private readonly IMessagingHubSender _sender;

        /// <summary>
        /// Initializes a new instance of the <see cref="TextcMessageReceiverBuilder"/> class.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="outputProcessor">The output processor.</param>
        /// <param name="syntaxParser">The syntax parser.</param>
        /// <param name="expressionScorer">The expression scorer.</param>
        /// <param name="cultureProvider">The culture provider.</param>
        public TextcMessageReceiverBuilder(
            IMessagingHubSender sender, 
            IOutputProcessor outputProcessor = null, 
            ISyntaxParser syntaxParser = null,
            IExpressionScorer expressionScorer = null, 
            ICultureProvider cultureProvider = null)
        {
            _sender = sender;
            _outputProcessor = outputProcessor ?? new MessageOutputProcessor(sender);
            _syntaxParser = syntaxParser ?? new SyntaxParser();
            _expressionScorer = expressionScorer ?? new RatioExpressionScorer();
            _cultureProvider = cultureProvider ?? new DefaultCultureProvider(CultureInfo.InvariantCulture);
            _commandProcessorFactories = new List<Func<IOutputProcessor, ICommandProcessor>>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TextcMessageReceiverBuilder"/> class.
        /// </summary>
        /// <param name="clientBuilder">The client builder.</param>
        /// <param name="outputProcessor">The output processor.</param>
        /// <param name="syntaxParser">The syntax parser.</param>
        /// <param name="expressionScorer">The expression scorer.</param>
        /// <param name="cultureProvider">The culture provider.</param>
        public TextcMessageReceiverBuilder(
            MessagingHubClientBuilder clientBuilder, 
            IOutputProcessor outputProcessor = null, 
            ISyntaxParser syntaxParser = null,
            IExpressionScorer expressionScorer = null, 
            ICultureProvider cultureProvider = null)
            :this(clientBuilder.Build(), outputProcessor, syntaxParser, expressionScorer, cultureProvider)
        {
         
        }

        /// <summary>
        /// Adds a new command syntax to the <see cref="TextcMessageReceiver"/> builder.
        /// </summary>
        /// <param name="syntaxPattern">The CSDL statement. Please refer to <seealso cref="https://github.com/takenet/textc-csharp#csdl"/> about the notation.</param>
        /// <returns></returns>
        public SyntaxTextcMessageReceiverBuilder ForSyntax(string syntaxPattern) => 
            ForSyntax(CsdlParser.Parse(syntaxPattern));

        /// <summary>
        /// Adds a new command syntax to the <see cref="TextcMessageReceiver"/> builder.
        /// </summary>
        /// <param name="culture">The syntax culture.</param>
        /// <param name="syntaxPattern">The CSDL statement. Please refer to <seealso cref="https://github.com/takenet/textc-csharp#csdl"/> about the notation.</param>        
        /// <returns></returns>
        public SyntaxTextcMessageReceiverBuilder ForSyntax(CultureInfo culture, string syntaxPattern) => 
            ForSyntax(CsdlParser.Parse(syntaxPattern, culture));

        /// <summary>
        /// Adds a new command syntax to the <see cref="TextcMessageReceiver"/> builder.
        /// </summary>
        /// <param name="syntax">The syntax instance to be added.</param>
        /// <returns></returns>
        public SyntaxTextcMessageReceiverBuilder ForSyntax(Syntax syntax) => 
            new SyntaxTextcMessageReceiverBuilder(_commandProcessorFactories, new List<Syntax> { syntax }, this);

        /// <summary>
        /// Adds multiple command syntaxes to the <see cref="TextcMessageReceiver"/> builder.
        /// The added syntaxes should be related and will be associated to a same command processor.
        /// </summary>
        /// <param name="syntaxPatterns">The CSDL statements. Please refer to <seealso cref="https://github.com/takenet/textc-csharp#csdl"/> about the notation.</param>
        /// <returns></returns>
        public SyntaxTextcMessageReceiverBuilder ForSyntaxes(params string[] syntaxPatterns) => 
            new SyntaxTextcMessageReceiverBuilder(_commandProcessorFactories, syntaxPatterns.Select(CsdlParser.Parse).ToList(), this);

        /// <summary>
        /// Adds multiple command syntaxes to the <see cref="TextcMessageReceiver"/> builder.
        /// The added syntaxes should be related and will be associated to a same command processor.
        /// </summary>
        /// <param name="culture">The syntaxes culture.</param>
        /// <param name="syntaxPatterns">The CSDL statements. Please refer to <seealso cref="https://github.com/takenet/textc-csharp#csdl"/> about the notation.</param>
        /// <returns></returns>
        public SyntaxTextcMessageReceiverBuilder ForSyntaxes(CultureInfo culture, params string[] syntaxPatterns) => 
            new SyntaxTextcMessageReceiverBuilder(_commandProcessorFactories, syntaxPatterns.Select(s => CsdlParser.Parse(s, culture)).ToList(), this);

        /// <summary>
        /// Adds multiple command syntaxes to the <see cref="TextcMessageReceiver"/> builder.
        /// The added syntaxes should be related and will be associated to a same command processor.
        /// </summary>
        /// <param name="syntaxes">The syntax instances to be added.</param>
        /// <returns></returns>
        public SyntaxTextcMessageReceiverBuilder ForSyntaxes(params Syntax[] syntaxes) => 
            new SyntaxTextcMessageReceiverBuilder(_commandProcessorFactories, syntaxes.ToList(), this);

        /// <summary>
        /// Sets the message text to be returned in case of no match of the user input.
        /// </summary>
        /// <param name="matchNotFoundMessage">The message text.</param>
        /// <returns></returns>
        public TextcMessageReceiverBuilder WithMatchNotFoundMessage(string matchNotFoundMessage) => 
            WithMatchNotFoundHandler(
                (message, receiver) =>
                    _sender.SendMessageAsync(matchNotFoundMessage, message.Pp ?? message.From, CancellationToken.None));

        /// <summary>
        /// Sets a handler to be called in case of no match of the user input.
        /// </summary>
        /// <param name="matchNotFoundHandler">The handler.</param>
        /// <returns></returns>
        public TextcMessageReceiverBuilder WithMatchNotFoundHandler(Func<Message, IMessageReceiver, Task> matchNotFoundHandler)
        {
            _matchNotFoundHandler = matchNotFoundHandler;
            return this;
        }

        /// <summary>
        /// Defines the user context validity, which is used to store the conversation variables.
        /// </summary>
        /// <param name="contextValidity">The context validity.</param>
        /// <returns></returns>
        public TextcMessageReceiverBuilder WithContextValidityOf(TimeSpan contextValidity) => 
            WithContextProvider(new ContextProvider(_cultureProvider, contextValidity));

        /// <summary>
        /// Defines a context provider to be used by the instance of <see cref="TextcMessageReceiver"/>.
        /// </summary>
        /// <param name="contextProvider">The context provider instance.</param>
        /// <returns></returns>
        public TextcMessageReceiverBuilder WithContextProvider(IContextProvider contextProvider)
        {
            if (contextProvider == null) throw new ArgumentNullException(nameof(contextProvider));
            _contextProvider = contextProvider;
            return this;
        }

        /// <summary>
        /// Defines a syntax parser to be used by the instance of <see cref="TextProcessor"/> for the current <see cref="TextcMessageReceiver"/>.
        /// </summary>
        /// <param name="syntaxParser">The syntax parser instance.</param>
        /// <returns></returns>
        public TextcMessageReceiverBuilder WithSyntaxParser(ISyntaxParser syntaxParser)
        {
            if (syntaxParser == null) throw new ArgumentNullException(nameof(syntaxParser));
            _syntaxParser = syntaxParser;
            return this;
        }

        /// <summary>
        /// Defines a expression scorer to be used by the instance of <see cref="TextProcessor"/> for the current <see cref="TextcMessageReceiver"/>.
        /// </summary>
        /// <param name="expressionScorer">The expression scorer instance.</param>
        /// <returns></returns>
        public TextcMessageReceiverBuilder WithExpressionScorer(IExpressionScorer expressionScorer)
        {
            if (expressionScorer == null) throw new ArgumentNullException(nameof(expressionScorer));
            _expressionScorer = expressionScorer;
            return this;
        }

        /// <summary>
        /// Defines a culture provider for the session contexts to be used by the instance of <see cref="TextProcessor"/> for the current <see cref="TextcMessageReceiver"/>.
        /// </summary>
        /// <param name="cultureProvider">The culture provider instance.</param>
        /// <returns></returns>
        public TextcMessageReceiverBuilder WithCultureProvider(ICultureProvider cultureProvider)
        {
            if (cultureProvider == null) throw new ArgumentNullException(nameof(cultureProvider));
            _cultureProvider = cultureProvider;
            return this;
        }

        /// <summary>
        /// Builds a new instance of <see cref="TextcMessageReceiver"/> using the defined configurations.
        /// </summary>
        /// <returns></returns>
        public TextcMessageReceiver Build()
        {
            var textProcessor = new TextProcessor(_syntaxParser, _expressionScorer);
            foreach (var commandProcessorFactory in _commandProcessorFactories)
            {
                textProcessor.CommandProcessors.Add(
                    commandProcessorFactory(_outputProcessor));
            }
            return new TextcMessageReceiver(
                _sender,
                textProcessor, 
                _contextProvider ?? new ContextProvider(_cultureProvider, TimeSpan.FromMinutes(5)), 
                _matchNotFoundHandler);
        }
        
        /// <summary>
        /// Builds a new instance of <see cref="TextcMessageReceiver"/> using the defined configurations and adds it to the associated <see cref="MessagingHubClient"/> instance.
        /// </summary>
        /// <returns></returns>
        public IMessagingHubClient BuildAndAddTextcMessageReceiver()
        {
            throw new NotImplementedException();
        }
    }
}
