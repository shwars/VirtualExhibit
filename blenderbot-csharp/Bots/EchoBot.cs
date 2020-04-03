// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio EchoBot v4.6.2

using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;

namespace PeopleBlenderBot.Bots
{
    public class EchoBot : ActivityHandler
    {
        protected string function_url = "https://coportrait.azurewebsites.net/api/pdraw?code=7mSfbEqAyLz45PpFv9CqKh5Q87TCrb7W7ePiNytwE2g9weEe0XH3PQ==";

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var Attachments = turnContext.Activity.Attachments;
            if (Attachments?.Count > 0)
            {
                var http = new HttpClient();
                var resp = await http.GetAsync(Attachments[0].ContentUrl);
                var str = await resp.Content.ReadAsStreamAsync();
                resp = await http.PostAsync(function_url, new StreamContent(str));
                var url = await resp.Content.ReadAsStringAsync();
                var msg = MessageFactory.Attachment(
                    (new HeroCard()
                    { Images = new CardImage[] { new CardImage(url) } }
                    ).ToAttachment());
                await turnContext.SendActivityAsync(msg);
            }
            else
            {
                await turnContext.SendActivityAsync("Please send picture");
            }

        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var welcomeText = "Hello and welcome!";
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text(welcomeText, welcomeText), cancellationToken);
                }
            }
        }
    }
}
