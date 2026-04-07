// ModuleType.cs
// Defines all possible module types a ship cell can contain.
// CSV keys are the single-character codes used in ship layout files.

namespace SpaceGame.Core
{
    public enum ModuleType
    {
        Empty,      // ' ' or '_' — no module, vacuum
        Engine,     // 'e' — provides speed and evasion
        Gun,        // 'g' — basic gun, low damage
        Cannon,     // 'c' — heavy cannon, high damage, slow
        Shield,     // 's' — absorbs damage before hull takes hits
        Hull,       // 'h' — structural, pure HP
        Reactor,    // 'r' — powers weapons; more reactors = faster fire rate
        Thruster,   // 't' — extra speed/evasion boost
        MissileBay, // 'm' — fires missiles, high burst damage
        LaserArray, // 'l' — fast, low damage beam weapon
    }

    public static class ModuleTypeExtensions
    {
        /// <summary>Parse a single CSV character into a ModuleType.</summary>
        public static ModuleType FromCode(string code)
        {
            return code.Trim().ToLower() switch
            {
                "e" => ModuleType.Engine,
                "g" => ModuleType.Gun,
                "c" => ModuleType.Cannon,
                "s" => ModuleType.Shield,
                "h" => ModuleType.Hull,
                "r" => ModuleType.Reactor,
                "t" => ModuleType.Thruster,
                "m" => ModuleType.MissileBay,
                "l" => ModuleType.LaserArray,
                _   => ModuleType.Empty,
            };
        }

        /// <summary>Convert a ModuleType back to its CSV character code.</summary>
        public static string ToCode(this ModuleType type)
        {
            return type switch
            {
                ModuleType.Engine     => "e",
                ModuleType.Gun        => "g",
                ModuleType.Cannon     => "c",
                ModuleType.Shield     => "s",
                ModuleType.Hull       => "h",
                ModuleType.Reactor    => "r",
                ModuleType.Thruster   => "t",
                ModuleType.MissileBay => "m",
                ModuleType.LaserArray => "l",
                _                     => "_",
            };
        }
    }
}
