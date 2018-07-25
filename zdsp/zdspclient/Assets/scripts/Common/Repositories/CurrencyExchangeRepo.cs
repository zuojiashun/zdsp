﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kopio.JsonContracts;
using Zealot.Common;
using Zealot.Repository;

namespace Zealot.Repository
{
    public static class CurrencyExchangeRepo
    {
        //dic_currencyExchange[numOfTime] = new List(){reqGold,rewardMoney};
        public static Dictionary<int, List<int>> dic_currencyExchange { get; private set; }

        public static void Init(GameDBRepo gameData)
        {
            dic_currencyExchange = new Dictionary<int, List<int>>();
            foreach (var entry in gameData.CurrencyExchange)
            {
                if (dic_currencyExchange.ContainsKey(entry.Value.exchangetimes) == false)
                    dic_currencyExchange.Add(entry.Value.exchangetimes, new List<int>() { entry.Value.exchangegold, entry.Value.exchangemoney });
            }
        }

        public static int GetReqGold(int exchangeTime)
        {
            if (dic_currencyExchange == null)
                return int.MaxValue;
            if(dic_currencyExchange.ContainsKey(exchangeTime))
                return dic_currencyExchange[exchangeTime][0];
            return dic_currencyExchange.Last().Value[0];
        }

        public static int GetRewardMoney(int exchangeTime)
        {
            if (dic_currencyExchange == null)
                return int.MinValue;
            if (dic_currencyExchange.ContainsKey(exchangeTime))
                return dic_currencyExchange[exchangeTime][1];
            return dic_currencyExchange.Last().Value[1];
        }      
    }
}