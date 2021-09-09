using System;
using System.Linq;
using static System.Array;
using static System.Math;

namespace Cricket_Fantasy_Planner
{
    public static class FantasyPlanner
    {
        public static int GetMaxActivePlayers(int[,] planner, int[] powerplayerIndex, int[] strengths, Match[] matches, int transfers, bool isReplanning, int numPlayers = 11)
        {
            int[][,] planners = new int[numPlayers][,];
            int[][] powerplayerIndices = new int[numPlayers][];
            int[] activePlayerCounts = new int[numPlayers];

            for (int i = 1; i <= numPlayers; i++)
            {
                int[,] currentPlanner = new int[matches.Length, strengths.Length];
                int[] currentPowerplayerIndex = new int[matches.Length];

                Copy(planner, currentPlanner, planner.Length);

                activePlayerCounts[i - 1] = GetActivePlayers(currentPlanner, currentPowerplayerIndex, strengths, matches, transfers, isReplanning, i, numPlayers);

                planners[i - 1] = currentPlanner;
                powerplayerIndices[i - 1] = currentPowerplayerIndex;
            }

            Copy(planners[GetMaximumValueAtIndex(activePlayerCounts)], planner, planner.Length);
            Copy(powerplayerIndices[GetMaximumValueAtIndex(activePlayerCounts)], powerplayerIndex, powerplayerIndex.Length);

            return activePlayerCounts.Max();
        }

        public static int GetActivePlayers(int[,] planner, int[] powerplayerIndex, int[] strengths, Match[] matches, int transfers, bool isReplanning, int playersPerMatch, int numPlayers)
        {
            int originalPlayersPerMatch = playersPerMatch;

            //If first match, then initial seed
            if (!isReplanning)
                InitialSeed(planner, powerplayerIndex, matches, strengths, playersPerMatch, numPlayers);

            decimal plannedSubstitutionsPerMatch = (decimal)transfers / (matches.Length - 1);
            int transfersMade = 0;

            for (int i = 1; i < matches.Length && transfers > 0; i++)
            {
                //Shadow players from previous match
                ShadowPrevious(planner, i, strengths.Length);

                //Fill active players
                bool isPowerplayerInFirst;

                int[] players = GetPlayersForTeams(matches[i], out isPowerplayerInFirst, playersPerMatch, strengths.ToArray());

                powerplayerIndex[i] = isPowerplayerInFirst ? matches[i].Team1 : matches[i].Team2;

                planner[i, matches[i].Team1] = planner[i - 1, matches[i].Team1] > players[0] ? planner[i - 1, matches[i].Team1] : players[0];
                planner[i, matches[i].Team2] = planner[i - 1, matches[i].Team2] > players[1] ? planner[i - 1, matches[i].Team2] : players[1];

                //Correction of exceeding number of players
                if (planner[i, matches[i].Team1] + planner[i, matches[i].Team2] > numPlayers)
                {
                    if (planner[i, matches[i].Team1] == planner[i - 1, matches[i].Team1])
                        planner[i, matches[i].Team2] -= planner[i, matches[i].Team1] + planner[i, matches[i].Team2] - numPlayers;
                    else
                        planner[i, matches[i].Team1] -= planner[i, matches[i].Team1] + planner[i, matches[i].Team2] - numPlayers;
                }

                //Calculate number of transfers required
                int numTransfers = GetSumOfArrayForIndex(planner, i) - GetSumOfArrayForIndex(planner, i - 1);

                //If run out of transfers, reduce number of players in teams in turns and then, recalculate powerplayer
                if (numTransfers > transfers)
                {
                    int teamIndex = 0;

                    while (numTransfers > transfers)
                    {
                        if (planner[i, matches[i].GetTeam(teamIndex)] > planner[i - 1, matches[i].GetTeam(teamIndex)])
                            planner[i, matches[i].GetTeam(teamIndex)]--;
                        else
                            planner[i, matches[i].GetTeam(1 - teamIndex)]--;

                        teamIndex = 1 - teamIndex;
                        numTransfers--;
                    }

                    if (powerplayerIndex[i] == matches[i].Team1 && planner[i, matches[i].Team1] == 0)
                        powerplayerIndex[i] = matches[i].Team2;
                    else if (powerplayerIndex[i] == matches[i].Team2 && planner[i, matches[i].Team2] == 0)
                        powerplayerIndex[i] = matches[i].Team1;
                }

                //Remove players from inactive teams with farthest games
                if (numTransfers > 0)
                {
                    int playersToRemove = numTransfers;
                    int numIterations = 0;

                    while (playersToRemove > 0)
                    {
                        int farthestTeam = GetNextFarthestTeam(matches, i, strengths.Length, numIterations);

                        if (planner[i - 1, farthestTeam] <= playersToRemove)
                        {
                            playersToRemove -= planner[i - 1, farthestTeam];
                            planner[i, farthestTeam] = 0;
                        }
                        else
                        {
                            planner[i, farthestTeam] = planner[i - 1, farthestTeam] - playersToRemove;
                            playersToRemove = 0;
                        }

                        numIterations++;
                    }

                    transfersMade += numTransfers;
                }

                //Change players per match based on substitutions done per match
                decimal actualSubstitutionsPerMatch = (decimal)transfersMade / i;

                if (actualSubstitutionsPerMatch > 0)
                    playersPerMatch = (int)Round(originalPlayersPerMatch * plannedSubstitutionsPerMatch / actualSubstitutionsPerMatch);
                else
                    playersPerMatch++;

                if (playersPerMatch < 1)
                    playersPerMatch = 1;
                else if (playersPerMatch > numPlayers)
                    playersPerMatch = numPlayers;

                transfers -= numTransfers;
            }

            return GetTotalPlaying(planner, matches);
        }

        private static int GetSumOfArrayForIndex(int[,] array, int i)
        {
            int sum = 0;

            for (int j = 0; j < array.GetLength(1); j++)
                sum += array[i, j];

            return sum;
        }

        private static void ShadowPrevious(int[,] planner, int i, int teamCount)
        {
            for (int j = 0; j < teamCount; j++)
                planner[i, j] = planner[i - 1, j];
        }

        private static void InitialSeed(int[,] planner, int[] powerplayerIndex, Match[] matches, int[] strengths, int playersPerMatch, int numPlayers)
        {
            while (GetSumOfArrayForIndex(planner, 0) < numPlayers)
            {
                Clear(planner, 0, planner.Length);

                int playersToAllocate = numPlayers;

                for (int i = 0; i < matches.Length && playersToAllocate > 0; i++)
                {
                    bool isPowerplayerInFirst;

                    int[] players = GetPlayersForTeams(matches[i], out isPowerplayerInFirst, playersPerMatch, strengths.ToArray());

                    if (i == 0)
                        powerplayerIndex[0] = isPowerplayerInFirst ? matches[i].Team1 : matches[i].Team2;

                    for (int j = 0; j < 2 && playersToAllocate > 0; j++)
                    {
                        //Check for exceeding number of players in a match
                        if (players[j] - planner[0, matches[i].GetTeam(j)] > playersToAllocate)
                            players[j] = playersToAllocate - planner[0, matches[i].GetTeam(j)];

                        playersToAllocate -= players[j] - planner[0, matches[i].GetTeam(j)];
                        planner[0, matches[i].GetTeam(j)] = players[j];
                    }
                }

                playersPerMatch++;
            }
        }

        private static int GetNextFarthestTeam(Match[] matches, int matchIndex, int teamsCount, int prevOccurrences = 0)
        {
            if (prevOccurrences == teamsCount - 2)
                throw new Exception("Inappropriate number of previous occurrences.");

            int[] farthestDistances = new int[teamsCount];

            for (int i = 0; i < farthestDistances.Length; i++)
                if (!matches[matchIndex].Contains(i))
                    farthestDistances[i] = int.MaxValue;

            int teamIndex;

            //Initialize farthest distances for each team except those that are already playing
            for (int i = matchIndex + 1; i < matches.Length; i++)
            {
                if (farthestDistances[matches[i].Team1] == int.MaxValue)
                    farthestDistances[matches[i].Team1] = i;

                if (farthestDistances[matches[i].Team2] == int.MaxValue)
                    farthestDistances[matches[i].Team2] = i;
            }

            int j = 0;

            while (true)
            {
                teamIndex = GetMaximumValueAtIndex(farthestDistances);

                if (j == prevOccurrences)
                    break;
                else
                {
                    j++;
                    farthestDistances[teamIndex] = 0;
                }
            }

            return teamIndex;
        }

        private static int GetMaximumValueAtIndex(int[] values)
        {
            int maximumIndex = 0;
            int maximumValue = int.MinValue;

            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] > maximumValue)
                {
                    maximumValue = values[i];
                    maximumIndex = i;
                }
            }

            return maximumIndex;
        }

        private static int[] GetPlayersForTeams(Match match, out bool isPowerplayerInFirst, int playersToPlay, int[] strengths)
        {
            int strength1 = strengths[match.Team1];
            int strength2 = strengths[match.Team2];

            if (match.Home == HomeState.First)
                strength1 = (int)Round(strength1 * 1.25);
            else if (match.Home == HomeState.Second)
                strength2 = (int)Round(strength2 * 1.25);

            int[] players = new int[2];

            players[0] = (int)Round((decimal)(playersToPlay * strength1) / (strength1 + strength2));
            players[1] = playersToPlay - players[0];

            if (strength1 > strength2 && players[0] < players[1])
                players[1]--;
            else if (strength1 < strength2 && players[0] > players[1])
                players[0]--;
            else if (strength1 == strength2)
            {
                if (players[0] > players[1])
                    players[0]--;
                else if (players[0] < players[1])
                    players[1]--;
            }

            if (players[0] > players[1])
                isPowerplayerInFirst = true;
            else if (players[1] > players[0])
                isPowerplayerInFirst = false;
            else if (match.Home == HomeState.First)
                isPowerplayerInFirst = true;
            else if (match.Home == HomeState.Second)
                isPowerplayerInFirst = false;
            else if (strength1 > strength2)
                isPowerplayerInFirst = true;
            else if (strength2 > strength1)
                isPowerplayerInFirst = false;
            else
                isPowerplayerInFirst = true;

            return players;
        }

        private static int GetTotalPlaying(int[,] planner, Match[] matches)
        {
            int totalPlaying = 0;

            for (int i = 0; i < matches.Length; i++)
                totalPlaying += planner[i, matches[i].Team1] + planner[i, matches[i].Team2];

            return totalPlaying;
        }
    }
}
