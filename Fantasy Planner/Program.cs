using System;
using System.Collections.Generic;
using System.Linq;
using Cricket_Fantasy_Planner;

namespace Fantasy_Planner
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;

            Console.Write("Enter number of players that cost substitutions: ");
            int numPlayers = int.Parse(Console.ReadLine());

            Console.WriteLine();

            Console.Write("Enter number of transfers allowed/remaining: ");
            int transfers = int.Parse(Console.ReadLine());

            string[] teams = new string[0];
            int[] strengths = new int[0];

            ReadTeams(ref teams, ref strengths);

            Match[] matches = GetSchedule(teams);

            int[,] planner = new int[matches.Length, teams.Length];

            bool isReplanning = GetReplanningStatus(teams, planner);

            int[] powerplayerIndex = new int[matches.Length];

            GetAllPlans(numPlayers, transfers, teams, strengths, matches, planner, isReplanning, powerplayerIndex);

            Array.Clear(planner, 0, planner.Length);
            Array.Clear(powerplayerIndex, 0, powerplayerIndex.Length);

            GetOptimumPlan(numPlayers, transfers, teams, strengths, matches, planner, isReplanning, powerplayerIndex);

            Console.ReadKey();
        }

        private static void GetOptimumPlan(int numPlayers, int transfers, string[] teams, int[] strengths, Match[] matches, int[,] planner, bool isReplanning, int[] powerplayerIndex)
        {
            Console.WriteLine("Maximum active players: " + FantasyPlanner.GetMaxActivePlayers(planner, powerplayerIndex, strengths.ToArray(), matches.ToArray(), transfers, isReplanning, numPlayers));

            DisplayPlanner(teams, matches, planner, powerplayerIndex, transfers);
        }

        private static void GetAllPlans(int numPlayers, int transfers, string[] teams, int[] strengths, Match[] matches, int[,] planner, bool isReplanning, int[] powerplayerIndex)
        {
            for (int i = 1; i <= numPlayers; i++)
            {
                Array.Clear(planner, 0, planner.Length);
                Array.Clear(powerplayerIndex, 0, powerplayerIndex.Length);

                int activePlayers = FantasyPlanner.GetActivePlayers(planner, powerplayerIndex, strengths.ToArray(), matches.ToArray(), transfers, isReplanning, i, numPlayers);

                Console.WriteLine("\nStarted with " + i + " players per match");

                DisplayPlanner(teams, matches, planner, powerplayerIndex, transfers);

                Console.WriteLine("Active players: " + activePlayers);
            }
        }

        private static bool GetReplanningStatus(string[] teams, int[,] planner)
        {
            bool isReplanning = false;

            Console.Write("\nAre you replanning this league? (y/n): ");

            if (Console.ReadKey().Key == ConsoleKey.Y)
            {
                isReplanning = true;

                Console.WriteLine("\nPlease ensure that the first match you entered in the schedule has already been played. And all other matches in the schedule are yet to be played.\n");

                for (int i = 0; i < teams.Length; i++)
                {
                    Console.Write(teams[i] + ": ");
                    planner[0, i] = int.Parse(Console.ReadLine());
                }
            }

            Console.WriteLine();

            return isReplanning;
        }

        private static void DisplayPlanner(string[] teams, Match[] matches, int[,] planner, int[] powerplayerIndex, int transfers)
        {
            Console.Write("\nPLANNER\n\n");

            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.Black;

            DrawCell("#", 3, 1, true);
            for (int i = 0; i < teams.Length; i++)
                DrawCell(teams[i]);

            DrawCell("Play", 6);
            DrawCell("Subs", 6);
            Console.WriteLine();

            for (int i = 0; i < matches.Length; i++)
            {
                DrawCell(i + 1, 3, 1, true);

                int playing = 0;

                for (int j = 0; j < teams.Length; j++)
                {
                    if (matches[i].Contains(j))
                    {
                        Console.BackgroundColor = ConsoleColor.Yellow;

                        if (matches[i].Home != HomeState.Neither)
                        {
                            if ((matches[i].Home == HomeState.First && matches[i].Team1 == j) || (matches[i].Home == HomeState.Second && matches[i].Team2 == j))
                                Console.BackgroundColor = ConsoleColor.Green;
                        }

                        playing += planner[i, j];
                    }

                    if (i > 0)
                    {
                        if (planner[i, j] > planner[i - 1, j])
                        {
                            transfers -= planner[i, j] - planner[i - 1, j];
                            Console.ForegroundColor = ConsoleColor.DarkGreen;
                        }
                        else if (planner[i, j] < planner[i - 1, j])
                            Console.ForegroundColor = ConsoleColor.DarkRed;
                    }

                    DrawCell(planner[i, j].ToString() + (powerplayerIndex[i] == j ? "^" : string.Empty));

                    Console.BackgroundColor = ConsoleColor.White;
                    Console.ForegroundColor = ConsoleColor.Black;
                }

                DrawCell(playing, 6);

                DrawCell(transfers, 6);

                Console.WriteLine();
            }
        }

        private static void DrawCell(int p, int colWidth = 5, int boundary = 1, bool IsFirstCellOfRow = false)
        {
            DrawCell(p.ToString(), colWidth, boundary, IsFirstCellOfRow);
        }

        private static void DrawCell(string p, int colWidth = 5, int boundary = 1, bool IsFirstCellOfRow = false)
        {
            if (p.Length > colWidth)
                p = p.Substring((p.Length - colWidth) / 2, colWidth);

            int leadingSpaces = (colWidth - p.Length) / 2;
            int trailingSpaces = colWidth - p.Length - leadingSpaces;

            if (!IsFirstCellOfRow)
                DrawBoundary(boundary);

            Console.Write(new string(' ', leadingSpaces) + p + new string(' ', trailingSpaces));
        }

        private static void DrawBoundary(int boundary)
        {
            ConsoleColor background = Console.BackgroundColor;
            Console.BackgroundColor = ConsoleColor.DarkGray;

            Console.Write(new string(' ', boundary));

            Console.BackgroundColor = background;
        }

        private static Match[] GetSchedule(string[] teams)
        {
            Console.WriteLine("\nEnter schedule in the following format: <Team Number>,<Team Number> (Example: 2,5). If one of the team has a home ground, append a + before its number (3,+5). Leave blank when done.\n");

            Match[] matches = new Match[0];
            string matchString;
            while (!string.IsNullOrEmpty(matchString = Console.ReadLine()))
            {
                string[] splitMatchString = matchString.Split(',');
                Match match = new Match();

                match.Team1 = int.Parse(splitMatchString[0]) - 1;
                match.Team2 = int.Parse(splitMatchString[1]) - 1;

                if (splitMatchString[0].Contains('+'))
                    match.Home = HomeState.First;
                else if (splitMatchString[1].Contains('+'))
                    match.Home = HomeState.Second;
                else
                    match.Home = HomeState.Neither;

                Array.Resize(ref matches, matches.Length + 1);
                matches[matches.Length - 1] = match;
            }

            Console.WriteLine("SCHEDULE\n");

            for (int i = 0; i < matches.Length; i++)
                Console.WriteLine((i + 1) + ". " + (matches[i].Home == HomeState.First ? "+" : string.Empty) + teams[matches[i].Team1] + " vs " + (matches[i].Home == HomeState.Second ? "+" : string.Empty) + teams[matches[i].Team2]);

            return matches;
        }

        private static void ReadTeams(ref string[] teams, ref int[] strengths)
        {
            Console.WriteLine("\nEnter teams information. Leave team name blank when done.\n");
            Console.Write("Short Team Name: ");

            string team;
            while (!string.IsNullOrEmpty(team = Console.ReadLine()))
            {
                Array.Resize(ref teams, teams.Length + 1);
                teams[teams.Length - 1] = team;

                Console.Write("Relative Strength: ");

                Array.Resize(ref strengths, teams.Length);
                strengths[strengths.Length - 1] = int.Parse(Console.ReadLine());

                Console.WriteLine();
                Console.Write("Short Team Name: ");
            }

            Console.WriteLine("\nTEAMS\tSTRENGTH");

            for (int i = 0; i < teams.Length; i++)
                Console.WriteLine((i + 1) + ". " + teams[i] + "\t" + strengths[i]);
        }
    }
}
