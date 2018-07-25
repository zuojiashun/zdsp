﻿using Kopio.JsonContracts;
using System.Collections.Generic;
using System.Linq;

namespace Zealot.Repository
{
    public static class OnsenRepo
    {
        public static List<OnsenJson> m_OnsenData;

        static OnsenRepo()
        {
            m_OnsenData = new List<OnsenJson>();
        }

        public static void Init(GameDBRepo gameData)
        {
            foreach (KeyValuePair<int, OnsenJson> entry in gameData.Onsen)
                m_OnsenData.Add(entry.Value);
        }

        public static List<OnsenJson> GetOnsenInfo()
        {
            return m_OnsenData;
        }
    }
}