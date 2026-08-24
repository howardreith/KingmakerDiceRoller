# Kingmaker Dice Roller 0.1.2

Version 0.1.2 fixes rolled ability scores reverting after main-campaign
mercenary recruitment. Rolled base values now pass through Kingmaker's
authoritative mercenary finalization path. Dice Roller verifies the
final stable mercenary descriptor rather than only the transient creation
preview.

The collapsed **Roll Stats** button now uses safe
bottom-centered allocator geometry instead of the incorrect upper-right
fallback. Ordinary Kingmaker base ability values and separate racial modifiers
are preserved, along with main-character creation, point-buy restoration,
cancellation, and unsupported-context isolation.
