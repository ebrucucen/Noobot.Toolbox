﻿using System;
using System.Collections.Generic;
using System.Linq;
using FlickrNet;
using Noobot.Core.Configuration;
using Noobot.Core.MessagingPipeline.Middleware;
using Noobot.Core.MessagingPipeline.Middleware.ValidHandles;
using Noobot.Core.MessagingPipeline.Request;
using Noobot.Core.MessagingPipeline.Response;
using Noobot.Core.Plugins.StandardPlugins;

namespace Noobot.Toolbox.Middleware
{
    /// <summary>
    /// Requires config entry 'flickr:apiKey' to be populated
    /// </summary>
    public class FlickrMiddleware : MiddlewareBase
    {
        private readonly IConfigReader _configReader;
        private readonly StatsPlugin _statsPlugin;

        public FlickrMiddleware(IMiddleware next, IConfigReader configReader, StatsPlugin statsPlugin) : base(next)
        {
            _configReader = configReader;
            _statsPlugin = statsPlugin;

            HandlerMappings = new[]
            {
                new HandlerMapping
                {
                    ValidHandles = ExactMatchHandle.For( "flickr", "pic"),
                    Description = "Finds a pics from flickr - usage: /flickr birds",
                    EvaluatorFunc = FlickrHandler,
                }
            };
        }

        private IEnumerable<ResponseMessage> FlickrHandler(IncomingMessage message, IValidHandle matchedHandle)
        {
            string searchTerm = message.TargetedText.Substring(matchedHandle.HandleHelpText.Length).Trim();

            if (string.IsNullOrEmpty(searchTerm))
            {
                yield return message.ReplyToChannel($"Please give me something to search, e.g. {matchedHandle} trains");
            }
            else
            {
                yield return message.IndicateTypingOnChannel();
                string apiKey = _configReader.GetConfigEntry<string>("flickr:apiKey");

                if (string.IsNullOrEmpty(apiKey))
                {
                    _statsPlugin.IncrementState("Flickr:Failed");
                    yield return message.ReplyToChannel("Woops, looks like a Flickr API Key has not been entered. Please ask the admin to fix this");
                }
                else
                {
                    var flickr = new Flickr(apiKey);

                    var options = new PhotoSearchOptions { Tags = searchTerm, PerPage = 50, Page = 1 };
                    PhotoCollection photos = flickr.PhotosSearch(options);

                    if (photos.Any())
                    {
                        _statsPlugin.IncrementState("Flickr:Sent");

                        int i = new Random().Next(0, photos.Count);
                        Photo photo = photos[i];
                        var attachment = new Attachment
                        {
                            AuthorName = photo.OwnerName,
                            Fallback = photo.Description,
                            ImageUrl = photo.LargeUrl,
                            ThumbUrl = photo.ThumbnailUrl
                        };

                        yield return message.ReplyToChannel($"Here is your picture about '{searchTerm}'", attachment);
                    }
                    else
                    {
                        _statsPlugin.IncrementState("Flickr:Failed");
                        yield return message.ReplyToChannel($"Sorry @{message.Username}, I couldn't find anything about {searchTerm}");
                    }
                }
            }
        }
    }
}