# Unofficial Steam Authenticator

You can search the windows app store for "Unofficial Steam Authenticator" or go here:  
http://windowsphone.com/s?appid=edcda38f-1eec-4f21-a267-190b4222f456  

--

Since valve recently introduced trade holds, requiring users to register their phone to their account and use an app to confirm
all actions with their items, there have been multiple complaints that they had no app for windows phone. Valve also made
several statements to the effect that they would not be making one.

--

This app uses a fork of [geel9](//github.com/geel9)'s SteamAuth library. Modified to be asynchronous so as not to lock up the phone while doing
things.

This app talks directly to steam's servers, your password is encrypted during this in the same way as the official steam client.
No data is sent to me or any other third party. If you lose access to your account the only way to get it back is with your
revocation code which is shown to you before you link the authenticator. **WRITE IT DOWN**. With it you can revoke the
authenticator at [store.steampowered.com/twofactor/manage](//store.steampowered.com/twofactor/manage).

If you can't login you need to visit [help.steampowered.com](//help.steampowered.com) first and click "I deleted or lost my Steam Guard Mobile Authenticator" you can get a login code texted to you.

--
I maintain this in my free time. If you have a problem / would like a feature added [open an issue here](//github.com/Top-Cat/UnofficialSteamAuthenticator/issues) or make the changes yourself and [submit a pull request](//github.com/Top-Cat/UnofficialSteamAuthenticator/pulls).

If you're just happy this exists you can donate me a drink at: [paypal.me/iamtopcat](//paypal.me/iamtopcat)

# Translations

Community translations provided by:

- German - https://steamcommunity.com/id/setoy
- Spanish - https://steamcommunity.com/id/3xlneet
- Russian - https://steamcommunity.com/id/Jericho_One

If you are *fluent* in a language that isn't listed and would like to help please [send me a message](//steamcommunity.com/id/top-cat) <3~
