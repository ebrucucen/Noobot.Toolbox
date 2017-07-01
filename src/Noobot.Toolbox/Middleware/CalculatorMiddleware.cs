﻿using System;
using System.Collections.Generic;
using Noobot.Core.MessagingPipeline.Middleware;
using Noobot.Core.MessagingPipeline.Middleware.ValidHandles;
using Noobot.Core.MessagingPipeline.Request;
using Noobot.Core.MessagingPipeline.Response;
using Noobot.Core.Plugins.StandardPlugins;
using xFunc.Maths;
using xFunc.Maths.Expressions;

namespace Noobot.Toolbox.Middleware
{
    public class CalculatorMiddleware : MiddlewareBase
    {
        private readonly StatsPlugin _statsPlugin;

        public CalculatorMiddleware(IMiddleware next, StatsPlugin statsPlugin) : base(next)
        {
            _statsPlugin = statsPlugin;
            HandlerMappings = new[]
            {
                new HandlerMapping
                {
                    ValidHandles = ExactMatchHandle.For("calc"),
                    Description = "Calculate mathematical expressions - usage: calc ((1+2)*3)/4",
                    EvaluatorFunc = CalculateHandler,
                    MessageShouldTargetBot = false
                },
                new HandlerMapping
                {
                    ValidHandles = new IValidHandle[] { new AlwaysMatchHandle() },
                    Description = "Try to calculate mathematical expressions without the 'calc' prefix - usage: ((1+2)*3)/4",
                    EvaluatorFunc = CalculateHandler,
                    MessageShouldTargetBot = false,
                    ShouldContinueProcessing = true,
                    VisibleInHelp = false
                }
            };
        }

        private IEnumerable<ResponseMessage> CalculateHandler(IncomingMessage message, IValidHandle matchedHandle)
        {
            string response = string.Empty;

            if (matchedHandle != null)
            {
                string expression = message.FullText.Substring(matchedHandle.HandleHelpText.Length).Trim();
                Parser parser = new Parser();

                try
                {
                    IExpression parsedExpression = parser.Parse(expression);
                    object result = parsedExpression.Calculate();
                    response = $"{parsedExpression} = {result}";
                }
                catch (Exception e)
                {
                    bool showErrors = !string.IsNullOrEmpty(matchedHandle.HandleHelpText);

                    if (showErrors)
                    {
                        _statsPlugin.IncrementState("Calc:Failed");
                        response = $"Who taught you maths? {e.Message}";
                    }
                }
            }

            if (!string.IsNullOrEmpty(response))
            {
                _statsPlugin.IncrementState("Calc:Calced");
                yield return message.ReplyToChannel($"@{message.Username}: {response}");
            }
        }
    }
}