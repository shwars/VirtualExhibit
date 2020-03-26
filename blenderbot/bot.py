# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

from botbuilder.core import ActivityHandler, TurnContext, MessageFactory, CardFactory
from botbuilder.schema import ChannelAccount, HeroCard, CardImage
import requests
import random

api_url = "https://coportrait.azurewebsites.net/api/pdraw?code=7mSfbEqAyLz45PpFv9CqKh5Q87TCrb7W7ePiNytwE2g9weEe0XH3PQ=="

welcome_urls = [
    "http://soshnikov.com/images/art/PhoBoGuy.png",
    "http://soshnikov.com/images/art/PhoBoGuy1.png",
    "http://soshnikov.com/images/art/olgaza.jpg",
    "http://soshnikov.com/images/art/irari.jpg",
    "http://soshnikov.com/images/art/oaki.jpg",
    "http://soshnikov.com/images/art/mama.jpg"
]
welcome_text = """
This bot is an interactive art exhibit that creates a Cognitive Portrait http://aka.ms/cognitive. Send your photograph to the bot and it will be blended together with 10 last photographs received by bot from different people.
"""
photo_msg = """
This bot loves photographs of people! Please send one!
**NB**: By uploading your photograph you give us the right to store it, process it, and to use in creating peopleblending portraits for other people. Do not upload your photograph if you do not agree with this! **Do not upload any photographs that are private, explicit, or demonstrate violence!** 
"""

class MyBot(ActivityHandler):
    # See https://aka.ms/about-bot-activity-message to learn more about the message and other activity types.

    async def send_welcome(self, turn_context: TurnContext):
        message = MessageFactory.attachment(
        CardFactory.hero_card(
            HeroCard(title=welcome_text,
                    images=[CardImage(url=random.choice(welcome_urls))])))
        await turn_context.send_activity(message)

    async def on_message_activity(self, turn_context: TurnContext):
        a = turn_context.activity
        if a.attachments is not None and len(a.attachments)>0:
            url = a.attachments[0].content_url
            r = requests.get(url)
            res = requests.post(api_url,data=r.content)
            url = res.text
            message = MessageFactory.attachment(
                CardFactory.hero_card(
                    HeroCard(title="Here is your cognitive portrait",
                    images=[CardImage(url=url)])))
            await turn_context.send_activity(message)
        else:
            await turn_context.send_activity(photo_msg)
            await self.send_welcome(turn_context)

    async def on_members_added_activity(
        self,
        members_added: ChannelAccount,
        turn_context: TurnContext
    ):
        for member_added in members_added:
            if member_added.id != turn_context.activity.recipient.id:
                await send_welcome(turn_context)
