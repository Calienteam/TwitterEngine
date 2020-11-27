﻿namespace TwitterEngine.Types

open Akkling

type AccountCredentials = {
    username : string;
    passwordHash : byte[];
    salt : byte[];
}

type Account = {
    credentials : AccountCredentials;
    isLoggedIn : bool;
}

type Credentials = {
    username : string;
    password : string;
}

type Tweet = {
    data : string;
    author : Account // todo account includes credentials is it needed? revise data structures
    sender : Account
}

type TweetSubscription =
| Hashtag of string
| Mention of string
| Sender of username: string

type UserRequest = 
| Login of password : string
| SendTweet of string
| ForwardTweet of Tweet
| ReceivedTweet of (Tweet * TweetSubscription)

type SubscriptionActorRequest = 
| NewSubscription of IActorRef<UserRequest>
| LoadHistoricalTweets of IActorRef<UserRequest>
| Tweet of Tweet

type SuperviserRequest = 
| Signup of Credentials
| UserRequest of (UserRequest * string)
| Subscribe of (TweetSubscription * string)
| Tweet of Tweet
| LoadHistoricalTweets of (TweetSubscription * string)

type OperationStatusResponse = 
| Success
| Error of string