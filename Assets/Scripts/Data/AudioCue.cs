namespace Warblade.Data
{
    /// <summary>
    /// Presentation sound identifiers used by gameplay, UI, and music systems.
    /// </summary>
    public enum AudioCue
    {
        None = 0,

        PlayerShoot = 10,
        PlayerShieldHit = 12,
        PlayerDeath = 13,

        EnemyShoot = 30,
        EnemyHit = 31,
        EnemyDeath = 32,
        EnemyMotherDeath = 33,
        EnemyKamikazeSpawn = 34,
        EnemyBonusSpecialSpawn = 35,

        BossIntro = 50,
        BossHit = 51,
        BossShoot = 52,
        BossDeath = 53,

        PickupCash = 70,
        PickupWeapon = 71,
        PickupLife = 72,
        PickupSucker = 73,
        PickupOther = 74,

        UiButton = 100,
        ShopOpen = 101,
        ShopBuySuccess = 102,
        ShopLeave = 104,
        Pause = 105,
        GameOver = 108,
        SectionWarp = 109,

        MusicMenu = 200,
        MusicGameplay = 201,
        MusicBoss = 202,
        MusicGameOver = 203
    }
}
