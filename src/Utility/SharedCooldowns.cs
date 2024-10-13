using System;
using System.Collections.Generic;

namespace Microdancer
{
    public class SharedCooldown
    {
        public string Action;
        public double Cooldown;
        public int Charges;

        public SharedCooldown(
            string action,
            float cooldown,
            int charges)
        {
            Action = action.ToLowerInvariant();
            Cooldown = cooldown;
            Charges = charges;
        }
    }

    public static class SharedCooldowns
    {
        public static SharedCooldown[][] Columns;
        public static Dictionary<string, int> Lookup = new Dictionary<string, int>();

        static SharedCooldowns()
        {
            Columns = ArrayExtensions.CreateJaggedArray<SharedCooldown[][]>(27, 22)!;

            Columns[0][0] = new("Celestial Intersection", 30, 2);
            Columns[0][1] = new("Lance Charge", 60, 1);
            Columns[0][2] = new("Excogitation", 45, 1);
            Columns[0][3] = new("Divine Benison", 30, 2);

            Columns[1][0] = new("Macrocosmos", 180, 2);
            Columns[1][1] = new("Summon Seraph", 120, 1);
            Columns[1][2] = new("Liturgy of the Bell", 180, 2);

            Columns[2][0] = new("Exaltation", 60, 1);
            Columns[2][1] = new("Salted Earth", 90, 2);
            Columns[2][2] = new("Camouflage", 90, 1);
            Columns[2][3] = new("Mantra", 90, 1);
            Columns[2][4] = new("Shukuchi", 60, 2);
            Columns[2][5] = new("Bulwark", 90, 1);
            Columns[2][6] = new("Recitation", 60, 1);
            Columns[2][7] = new("Thrill of Battle", 90, 1);
            Columns[2][8] = new("Rhizomata", 90, 1);

            Columns[3][0] = new("Horoscope", 60, 2);
            Columns[3][1] = new("Raging Strikes", 120, 1);
            Columns[3][2] = new("Avail", 120, 1);
            Columns[3][3] = new("En Avant", 30, 3);
            Columns[3][4] = new("Life Surge", 40, 2);
            Columns[3][5] = new("Dark Missionary", 90, 1);
            Columns[3][6] = new("Heart of Light", 90, 1);
            Columns[3][7] = new("Thunderclap", 30, 3);
            Columns[3][8] = new("Divine Veil", 90, 1);
            Columns[3][9] = new("Whispering Dawn", 60, 1);
            Columns[3][10] = new("Shake It Off", 90, 1);
            Columns[3][11] = new("Asylum", 90, 1);
            Columns[3][12] = new("Soteria", 60, 1);
            Columns[3][13] = new("Vicepit", 40, 2);

            Columns[4][0] = new("Collective Unconscious", 60, 1);
            Columns[4][1] = new("Cold Fog", 90, 1);
            Columns[4][2] = new("Bow Shock", 60, 1);
            Columns[4][3] = new("Riddle of Fire", 60, 1);
            Columns[4][4] = new("Inner Release", 60, 1);
            Columns[4][5] = new("Aquaveil", 60, 1);
            Columns[4][6] = new("Physis", 60, 1);
            Columns[4][7] = new("Physis II", 60, 1);

            Columns[5][0] = new("Neutral Sect", 120, 1);
            Columns[5][1] = new("Nature's Minne", 120, 1);
            Columns[5][2] = new("Manaward", 120, 1);
            Columns[5][3] = new("Force Field", 120, 1);
            Columns[5][4] = new("Shield Samba", 90, 1);
            Columns[5][5] = new("Battle Litany", 120, 1);
            Columns[5][6] = new("Living Shadow", 120, 1);
            Columns[5][7] = new("Great Nebula", 120, 1);
            Columns[5][8] = new("Tactician", 90, 1);
            Columns[5][9] = new("Passage of Arms", 120, 1);
            Columns[5][10] = new("Fey Illumination", 120, 1);
            Columns[5][11] = new("Vengeance", 120, 1);
            Columns[5][12] = new("Damnation", 120, 1);
            Columns[5][13] = new("Temperance", 120, 1);
            Columns[5][14] = new("Panhaima", 120, 1);
            Columns[5][15] = new("Arcane Circle", 120, 1);
            Columns[5][16] = new("Tempera Coat", 120, 1);

            Columns[6][0] = new("Lightspeed", 60, 2);
            Columns[6][1] = new("Battle Voice", 120, 1);
            Columns[6][2] = new("Triplecast", 60, 2);
            Columns[6][3] = new("Both Ends", 120, 1);
            Columns[6][4] = new("Nightbloom", 120, 1);
            Columns[6][5] = new("Improvisation", 120, 2);
            Columns[6][6] = new("Oblation", 60, 2);
            Columns[6][7] = new("Magick Barrier", 120, 1);
            Columns[6][8] = new("Meikyo Shisui", 55, 2);
            Columns[6][9] = new("Expedient", 120, 1);
            Columns[6][10] = new("Thin Air", 60, 2);
            Columns[6][11] = new("Holos", 120, 1);

            Columns[7][0] = new("Divination", 120, 1);
            Columns[7][1] = new("Troubadour", 90, 1);
            Columns[7][2] = new("Being Mortal", 120, 1);
            Columns[7][3] = new("Devilment", 120, 1);
            Columns[7][4] = new("Shadowed Vigil", 120, 1);
            Columns[7][5] = new("Riddle of Earth", 120, 1);
            Columns[7][6] = new("Shade Shift", 120, 1);
            Columns[7][7] = new("Embolden", 120, 1);
            Columns[7][8] = new("Radiant Aegis", 60, 2);
            Columns[7][9] = new("Presence of Mind", 120, 1);
            Columns[7][10] = new("Haima", 120, 1);

            Columns[8][0] = new("Draw", 55, 1);
            Columns[8][1] = new("Level 5 Death", 180, 1);
            Columns[8][2] = new("Riddle of Wind", 90, 1);

            Columns[9][0] = new("Earthly Star", 60, 1);
            Columns[9][1] = new("Winged Reprobation", 90, 1);
            Columns[9][2] = new("Flamethrower", 60, 1);
            Columns[9][3] = new("Meditate", 60, 1);
            Columns[9][4] = new("Protraction", 60, 1);
            Columns[9][5] = new("Aetherial Shift", 60, 1);
            Columns[9][6] = new("Krasis", 60, 1);

            Columns[10][0] = new("Celestial Opposition", 60, 1);
            Columns[10][1] = new("Surpanaka", 30, 4);
            Columns[10][2] = new("Curing Waltz", 60, 1);
            Columns[10][3] = new("Equilibrium", 60, 1);
            Columns[10][4] = new("Slither", 30, 1);

            Columns[11][0] = new("Essential Dignity", 40, 3);
            Columns[11][1] = new("The Warden's Paean", 45, 1);
            Columns[11][2] = new("Delerium ", 60, 1);
            Columns[11][3] = new("No Mercy", 60, 1);
            Columns[11][4] = new("Kassatsu", 60, 1);
            Columns[12][5] = new("Fight or Flight", 60, 1);
            Columns[12][6] = new("Consolation", 30, 2);
            Columns[12][7] = new("Plenary Indulgence", 60, 1);

            Columns[13][0] = new("Synastry", 120, 1);
            Columns[13][1] = new("Barrage", 120, 1);
            Columns[13][2] = new("Ley Lines", 120, 1);
            Columns[13][3] = new("Sea Shanty", 120, 1);
            Columns[13][4] = new("Technical Step", 120, 3);
            Columns[13][5] = new("Aurora", 60, 2);
            Columns[13][6] = new("Brotherhood", 120, 1);
            Columns[13][7] = new("Ten Chi Jin", 120, 1);
            Columns[13][8] = new("Guardian", 120, 1);
            Columns[13][9] = new("Acceleration", 55, 2);
            Columns[13][10] = new("Deployment Tactics", 90, 1);
            Columns[13][11] = new("Searing Light", 120, 1);
            Columns[13][12] = new("Tetragrammaton", 60, 2);
            Columns[13][13] = new("Zoe", 90, 1);

            Columns[14][0] = new("Mountain Buster", 60, 1);
            Columns[14][1] = new("Elusive Jump", 30, 1);
            Columns[14][2] = new("Heart of Stone", 25, 1);
            Columns[14][3] = new("Heart of Corundum", 25, 1);
            Columns[14][4] = new("Smudge", 20, 1);

            Columns[15][0] = new("Glass Dance", 90, 1);
            Columns[15][1] = new("Dark Mind", 60, 1);
            Columns[15][2] = new("Orogeny", 30, 1);
            Columns[15][3] = new("Soul Scythe", 30, 2);

            Columns[16][0] = new("Living Dead", 300, 1);
            Columns[16][1] = new("Superbolide", 360, 1);
            Columns[16][2] = new("Hallowed Grond", 420, 1);
            Columns[16][3] = new("Holmgang ", 240, 1);
            Columns[16][4] = new("Philosopha", 180, 1);

            Columns[17][0] = new("The Blackest Night", 15, 1);
            Columns[17][1] = new("Hide", 20, 1);
            Columns[17][2] = new("Pepsis", 30, 1);

            Columns[18][0] = new("Quasar", 60, 1);
            Columns[18][1] = new("Standard Step", 30, 2);
            Columns[18][2] = new("Circle of Scorn", 30, 1);
            Columns[18][3] = new("Tengentsu", 15, 1);
            Columns[18][4] = new("Emergency Tactics", 15, 1);
            Columns[18][5] = new("Bloodwhetting", 25, 1);
            Columns[18][6] = new("Icarus", 45, 1);
            Columns[18][7] = new("Arcane Crest", 30, 1);

            Columns[19][0] = new("Chelonian Gate", 30, 1);
            Columns[19][1] = new("Ninjutsu", 20, 2);
            Columns[19][2] = new("Kerachole", 30, 1);

            Columns[20][0] = new("Closed Position", 30, 1);
            Columns[20][1] = new("Assize", 45, 1);
            Columns[20][2] = new("Taurochole", 45, 1);

            Columns[21][0] = new("Peculiar Light", 60, 1);
            Columns[21][1] = new("Ixochole", 30, 1);
            Columns[21][2] = new("Hell's Egress", 20, 1);

            Columns[22][0] = new("Transpose", 5, 1);
            Columns[22][1] = new("Eruption", 30, 1);
            Columns[22][2] = new("Kardia", 5, 1);

            Columns[23][0] = new("Second Wind", 120, 1);

            Columns[24][0] = new("Reprisal", 60, 1);
            Columns[24][1] = new("Lucid Dreaming", 60, 1);

            Columns[25][0] = new("Bloodbath", 90, 1);
            Columns[25][1] = new("Rampart", 90, 1);

            Columns[26][0] = new("Arm's Length", 120, 1);
            Columns[26][1] = new("Surecast", 120, 1);

            // Populate dictionary
            for (var col = 0; col < Columns.Length; ++col)
            {
                for (var row = 0; row < Columns[col].Length; ++row)
                {
                    var cd = Columns[col][row];
                    if (cd != null)
                    {
                        Lookup[cd.Action] = col;
                    }
                }
            }
        }
    }
}