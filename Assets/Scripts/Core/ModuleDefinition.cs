// ModuleDefinition.cs
// Static lookup table of stats for every ModuleType.
// All balance values live here — single place to tweak numbers.

using UnityEngine;

namespace SpaceGame.Core
{
    [System.Serializable]
    public struct ModuleStats
    {
        public ModuleType Type;
        public string     DisplayName;
        public int        MaxHP;        // How much damage this module can take before it's destroyed
        public int        DamagePerRound; // Weapon modules: damage dealt per combat round
        public float      EvasionBonus;   // Engine/Thruster: % chance to dodge an incoming hit
        public int        ShieldHP;       // Shield modules: absorbs this much damage before failing
        public float      FireRateBonus;  // Reactor: multiplier on weapon fire rate
        public Color      DisplayColor;   // Color used when rendering the module as a square
        [TextArea] public string Description;
    }

    public static class ModuleDefinition
    {
        public static ModuleStats Get(ModuleType type)
        {
            return type switch
            {
                ModuleType.Engine => new ModuleStats
                {
                    Type          = ModuleType.Engine,
                    DisplayName   = "Engine",
                    MaxHP         = 20,
                    DamagePerRound= 0,
                    EvasionBonus  = 0.08f,
                    ShieldHP      = 0,
                    FireRateBonus = 0f,
                    DisplayColor  = new Color(0.2f, 0.8f, 0.2f),   // green
                    Description   = "Propels the ship. Each engine adds 8% evasion chance."
                },
                ModuleType.Gun => new ModuleStats
                {
                    Type          = ModuleType.Gun,
                    DisplayName   = "Gun",
                    MaxHP         = 15,
                    DamagePerRound= 5,
                    EvasionBonus  = 0f,
                    ShieldHP      = 0,
                    FireRateBonus = 0f,
                    DisplayColor  = new Color(0.9f, 0.3f, 0.1f),   // orange-red
                    Description   = "Basic ballistic weapon. 5 damage per round."
                },
                ModuleType.Cannon => new ModuleStats
                {
                    Type          = ModuleType.Cannon,
                    DisplayName   = "Cannon",
                    MaxHP         = 25,
                    DamagePerRound= 15,
                    EvasionBonus  = 0f,
                    ShieldHP      = 0,
                    FireRateBonus = 0f,
                    DisplayColor  = new Color(0.8f, 0.1f, 0.1f),   // dark red
                    Description   = "Heavy cannon. 15 damage per round, fires every other round."
                },
                ModuleType.Shield => new ModuleStats
                {
                    Type          = ModuleType.Shield,
                    DisplayName   = "Shield",
                    MaxHP         = 10,
                    DamagePerRound= 0,
                    EvasionBonus  = 0f,
                    ShieldHP      = 30,
                    FireRateBonus = 0f,
                    DisplayColor  = new Color(0.2f, 0.4f, 0.9f),   // blue
                    Description   = "Energy shield. Absorbs 30 damage before failing."
                },
                ModuleType.Hull => new ModuleStats
                {
                    Type          = ModuleType.Hull,
                    DisplayName   = "Hull",
                    MaxHP         = 40,
                    DamagePerRound= 0,
                    EvasionBonus  = 0f,
                    ShieldHP      = 0,
                    FireRateBonus = 0f,
                    DisplayColor  = new Color(0.5f, 0.5f, 0.5f),   // grey
                    Description   = "Structural plating. Pure hit points, no other function."
                },
                ModuleType.Reactor => new ModuleStats
                {
                    Type          = ModuleType.Reactor,
                    DisplayName   = "Reactor",
                    MaxHP         = 20,
                    DamagePerRound= 0,
                    EvasionBonus  = 0f,
                    ShieldHP      = 0,
                    FireRateBonus = 0.2f,
                    DisplayColor  = new Color(0.9f, 0.9f, 0.1f),   // yellow
                    Description   = "Power core. Each reactor adds 20% weapon fire rate."
                },
                ModuleType.Thruster => new ModuleStats
                {
                    Type          = ModuleType.Thruster,
                    DisplayName   = "Thruster",
                    MaxHP         = 15,
                    DamagePerRound= 0,
                    EvasionBonus  = 0.12f,
                    ShieldHP      = 0,
                    FireRateBonus = 0f,
                    DisplayColor  = new Color(0.1f, 0.9f, 0.8f),   // cyan
                    Description   = "Booster thruster. 12% evasion per thruster."
                },
                ModuleType.MissileBay => new ModuleStats
                {
                    Type          = ModuleType.MissileBay,
                    DisplayName   = "Missile Bay",
                    MaxHP         = 18,
                    DamagePerRound= 20,
                    EvasionBonus  = 0f,
                    ShieldHP      = 0,
                    FireRateBonus = 0f,
                    DisplayColor  = new Color(0.9f, 0.5f, 0.1f),   // amber
                    Description   = "Fires guided missiles. 20 damage, fires every 3 rounds."
                },
                ModuleType.LaserArray => new ModuleStats
                {
                    Type          = ModuleType.LaserArray,
                    DisplayName   = "Laser Array",
                    MaxHP         = 12,
                    DamagePerRound= 3,
                    EvasionBonus  = 0f,
                    ShieldHP      = 0,
                    FireRateBonus = 0f,
                    DisplayColor  = new Color(0.9f, 0.2f, 0.9f),   // magenta
                    Description   = "Rapid laser pulses. 3 damage but fires every round without penalty."
                },
                _ => new ModuleStats                                // Empty
                {
                    Type          = ModuleType.Empty,
                    DisplayName   = "Empty",
                    MaxHP         = 0,
                    DamagePerRound= 0,
                    EvasionBonus  = 0f,
                    ShieldHP      = 0,
                    FireRateBonus = 0f,
                    DisplayColor  = new Color(0.05f, 0.05f, 0.1f), // near-black
                    Description   = "Empty space."
                }
            };
        }
    }
}
