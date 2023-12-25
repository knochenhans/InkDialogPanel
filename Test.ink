VAR character_name = "Person"
VAR character_title_color = "\#ff00d0"
VAR character_body_color = "\#ffd0d0"

-> Dialog

=== Dialog
= Intro
You find yourself facing a painfully boring looking person. He smacks his lips and then grins.

“Well, seems you wound up in a test dialog. Welcome.” # Character
-> Main

= Main
+ [”Ah test dialog? For what?”] # Player
    “A test dialog for, erm… for a dialog system, I suppose?” # Character
  
    He shrugs and looks away, seemingly uninterested in any further conversation.

    + + [“Hey shitbird, I’m talking to you!“] # Player

        It is clear the man is hearing you, but at this point he doesn’t even care that he’s simply ignoring you.
        
        -> Close_Bad

    + + [\[Ignore him\]] # Player

+ [“Alright, a test dialog it is!”] # Player

    The person looks at you with sudden glee. # Character

    “Glad to see you like it here in this… test space.” His grin widens.

    + + [\[Also put on a grin\]]

        You both face each other grinning. Happy times!
        -> Close

    + + [“I lied, I actually don’t like it here even a bit!” \[Laugh deamonically\]]

        The man seems taken aback by your sudden change of mood. For a moment, he seems to have lost all his determination. #Character

        Finally, his face darkens as he looks you directly in the eye. “Go away” he grumbles.

        -> Close_Bad

* -> Close
- -> Main 

= Close
You don’t really have to say anything anymore. The dialog test space around you begins to fade peacefully.
-> END

= Close_Bad
You turn away and start to cry silently. Testing dialogs is not a joyful business.
-> END