using System.Collections.Generic;

namespace PuzzleBobble;
public class Levels
{
    private static readonly Dictionary<string, string> _repo = new()
    {
        {"10-8-fullCombo",
            """
            j k k k . . l l
             j l l i h h l
            j j i i . . h h
             . . f f g g g
            . . f f e e . .
             d d e e . . c
            d a a . . . c c
             . . a b b b c
            """
},
{"10-8-whatTheH",
            """
            a f . . . j b k
             f c c . . b k
            . . d d j a a .
             h a g . . e .
            b a g . . i f .
             d e . . . c a
            i e b . . . g h
             c i . . . . h
            """
},
{"12-10-cowIsCow",
            """
            . j j . . l l .
             j i . . . k l 
            j i . . . . k l
             i i . . . f k 
            . d h h h f k .
             d h g g f g g
            d d . g f . e e
             c d c c e e e
            . c c a a b b .
             . a a b b b . 
            """
},
{"12-10-structure",
            """
            e c c f i i b h
             l l f g b k i 
            . . . l d h . .
             . . h h d l .
            e e a a k . . g
             d a c g k . j 
            . . c i g j f f
             . . . b d j . 
            j j c d b b . .
             a a . . h g e
            """
},
{"12-12-seaHorse",
            """
            . e . d b . . e
             S . . h c g S
            . . a g . e . g
             h k . j . . b
            l h . . . a g k
             . . c h . k . 
            . . i d f l d .
             e a k . b j .
            . . b l f S . h
             . . . j d a i
            . f i . l c . .
             j c S f i . .
            """
},
{"2-16-chainedA",
            """
            . a a a a . b .
             . a . b . b . 
            . a . . . . b .
             . a b b b b . 
            """
},
{"2-2-littletwin",
            """
            a a a . . b b b
             b b . . . a a
            """
},
{"2-4-chainedB",
            """
            . a . b b b b .
             . a . a . b . 
            . a . . . . b .
             . a a a a b . 
            """
},
{"3-2-babycrown",
            """
            a . . a c . . b
             c b b c a a b
            """
},
{"3-2-base",
            """
            c a c b b a b a
             b b c a a c c
            """
},
{"3-4-connectHalf",
            """
            . . b c a a . .
             c a c . b a c
            a a . . . . c b
             b b . . . b c 
            """
},
{"3-4-connectHalf2",
            """
            . . a b b a . .
             c a c . c b b
            a c . . . . a c
             a b . . . c b
            """
},
{"3-4-connectHalf3",
            """
            . . a R b c . .
             b B c . b a a
            a c . . . . R b
             c b . . . c a
            """
},
{"4-10-Y2",
            """
            d d c . . R d b
             b c R . c c a
            . S S S S S S .
             . b a B c b .
            . . a c a b . .
             . . S S S . .
            . . . d B . . .
             . . c b b . .
            . . . S S . . .
             . . a a d . .
            """
},
{"4-6-Hat2",
            """
            . . . . . a . .
             . . . b d . .
            . . a a R . . .
             . c b b d . .
            . d c R d a c .
             a a d c b b c
            """
},
{"4-6-omega",
            """
            d b b . . . a d
             . a a a b b c
            c a b . . c d a
             b d c . d d c
            . b c d a a c .
             . d b d b c .
            """
},
{"4-6-omega3",
            """
            d a . . . c b a
             a R a c c b .
            c b b . . d d a
             d c d . b b d
            . d a a b B c .
             . b d a c c .
            """
},
{"5-6-Hat",
            """
            . . . . . d . .
             . . . a b . .
            . . b d e . . .
             . e a c a . .
            . a c e b d e .
             e d c a b c b
            """
},
{"5-6-Hat3",
            """
            . . . . . e . .
             . . . c d . .
            . . a b c . . .
             . a d a a . .
            . b c e b b e .
             b d d a c c e
            """
},
{"6-10-unknownEntity",
            """
            . . . b e . . g
             . c . a h d b
            a d . d f b h .
             b g e . c d h 
            . e b . f a . .
             . g b e . d e 
            d f a h c g c f
             b g c . . a b 
            . . . d f . e g
             f a e g . h c
            """
},
{"6-8-pyramid",
            """
            a a b b c c d d
             e e c c a a b
            . e f f d d b .
             . b c a a e .
            . . b c e e . .
             . . f f d . .
            . . . d d . . .
             . . . f . . . 
            """
},
{"6-8-theBox",
            """
            e a a g g b b e
             e c c b f g c
            g f . . . . a g
             a d . . . e a 
            d a . . . . d d
             b f . . . c f
            f a d b e a c g
             f c a g e f b
            """
},
{"8-10-Y",
            """
            d e b . . c a d
             a a f . e c g
            . h c c f e g .
             . h a b b f .
            . . f a d h . .
             . . f d e . .
            . . g b c . . .
             . . b c h . .
            . . e a a . . .
             . . e d g . .
            """
},
{"test-bombpass",
            """
            S S S S S S S S
             S S S S B S S 
            S S S B S S S S
             S B S S S S S 
            S a S S S S S S
             S a S S S S S 
            """
},
{"test-colorpass",
            """
            S S S S a R a S
             S S c S e S S 
            S S R e e S S S
             R a S S S S S 
            e S a S a S S S
             S a S a S a S 
            """
},
{"test-fallcollide",
            """
            b a a a a a a a
             d . c c c c a
            d . b b b b b a
             d . c c c c a
            d . b b b b b a
             d . c c c c a
            d . b b b b b a
             d . c c c c a
            d . b b b b b a
             d . . . . . a
            d d d d d d . .
             b . . . . . .
            """
},
{"test",
            """
            a a a a a a a a
             i i i i i i a
            h h h h h h h a
             g g g g g g a
            f f f f f f f a
             e e e e e e a
            d d d d d d d a
             c c c c c c a
            b b b b b b b a
             . . . . . . a
            . . . . . . . .
             . . . . . . .
            """
},
    };

    public static List<string> GetLevelNames()
    {
        return [.. _repo.Keys];
    }

    public static string GetLevel(string name)
    {
        return _repo[name];
    }
}