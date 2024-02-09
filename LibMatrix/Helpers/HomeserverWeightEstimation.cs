namespace LibMatrix.Helpers;

public class HomeserverWeightEstimation {
    public static Dictionary<string, int> EstimatedSize = new() {
        { "matrix.org", 843870 },
        { "anontier.nl", 44809 },
        { "nixos.org", 8195 },
        { "the-apothecary.club", 6983 },
        { "waifuhunter.club", 3953 },
        { "neko.dev", 2666 },
        { "nerdsin.space", 2647 },
        { "feline.support", 2633 },
        { "gitter.im", 2584 },
        { "midov.pl", 2219 },
        { "no.lgbtqia.zone", 2083 },
        { "nheko.im", 1883 },
        { "fachschaften.org", 1849 },
        { "pixelthefox.net", 1478 },
        { "arcticfoxes.net", 981 },
        { "pixie.town", 817 },
        { "privacyguides.org", 809 },
        { "rory.gay", 653 },
        { "artemislena.eu", 599 },
        { "alchemi.dev", 445 },
        { "jameskitt616.one", 390 },
        { "hackint.org", 382 },
        { "pikaviestin.fi", 368 },
        { "matrix.nomagic.uk", 337 },
        { "thearcanebrony.net", 178 },
        { "fairydust.space", 176 },
        { "grin.hu", 176 },
        { "envs.net", 165 },
        { "tastytea.de", 143 },
        { "koneko.chat", 121 },
        { "vscape.tk", 115 },
        { "funklause.de", 112 },
        { "seirdy.one", 107 },
        { "pcg.life", 72 },
        { "draupnir.midnightthoughts.space", 22 },
        { "tchncs.de", 19 },
        { "catgirl.cloud", 16 },
        { "possum.city", 16 },
        { "tu-dresden.de", 9 },
        { "fosscord.com", 9 },
        { "nightshade.fun", 8 },
        { "matrix.eclipse.org", 8 },
        { "masfloss.net", 8 },
        { "e2e.zone", 8 },
        { "hyteck.de", 8 }
    };

    public static Dictionary<string, int> LargeRooms = new() {
        { "!ehXvUhWNASUkSLvAGP:matrix.org", 21957 },
        { "!fRRqjOaQcUbKOfCjvc:anontier.nl", 19117 },
        { "!OGEhHVWSdvArJzumhm:matrix.org", 101457 },
        { "!YTvKGNlinIzlkMTVRl:matrix.org", 30164 }
    };
}