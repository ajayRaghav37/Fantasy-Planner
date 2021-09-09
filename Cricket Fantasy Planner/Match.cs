using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cricket_Fantasy_Planner
{
    public enum HomeState
    {
        Neither = 0,
        First = 1,
        Second = 2
    }

    public class Match
    {
        public int Team1;
        public int Team2;
        public HomeState Home;

        public bool Contains(int teamNumber)
        {
            if (Team1 == teamNumber || Team2 == teamNumber)
                return true;

            return false;
        }

        public int GetTeam(int index)
        {
            if (index == 0)
                return Team1;
            else
                return Team2;
        }
    }
}
