﻿using Kopio.JsonContracts;
using System.Collections.Generic;
using System.Linq;

namespace Zealot.Repository
{
    public struct WorldMapCountryMonster
    {
        public int archetype;
        //public int levelID;

        public WorldMapCountryMonster(int _archetype/*, int _levelID*/)
        {
            archetype = _archetype;
            //levelID = _levelID;
        }
    }

    public struct WorldMapCountryPlaceInterest
    {
        public string iconPath;
        public string name;
        public int levelID;
        public int interestID;  //Index to placeinterest in kopio

        public WorldMapCountryPlaceInterest(string _iconPath, string _name, int _levelID, int _interestID)
        {
            name = _name;
            levelID = _levelID;
            iconPath = _iconPath;
            interestID = _interestID;
        }
    }

    public struct WorldMapCountry
    {
        public string name;
        public string lvRange;
        public List<WorldMapCountryPlaceInterest> placeLst;
        public List<WorldMapCountryMonster> monLst;

        public WorldMapCountry(string regName, string regLvRange)
        {
            name = regName;
            lvRange = regLvRange;
            placeLst = new List<WorldMapCountryPlaceInterest>();
            monLst = new List<WorldMapCountryMonster>();
        }
    }

    public struct WorldMapInterestMarkerPos
    {
        public float x;
        public float y;
        public float z;

        public WorldMapInterestMarkerPos(float _x, float _y, float _z)
        {
            x = _x;
            y = _y;
            z = _z;
        }
    }

    public static class MapRepo
    {
        public static Dictionary<int, WorldMapCountry> mWorldMap = null;
        public static Dictionary<string, int> mWorldMap2 = null;
        public static List<WorldMapCountry> mWorldMapLst = null;
        public static List<WorldMapInterestMarkerPos> mWorldMapInterestMarkLst = null;

        static MapRepo()
        {
            mWorldMap = new Dictionary<int, WorldMapCountry>();
            mWorldMap2 = new Dictionary<string, int>();
            mWorldMapLst = new List<WorldMapCountry>();
            mWorldMapInterestMarkLst = new List<WorldMapInterestMarkerPos>();
        }
       
        public static void Init(GameDBRepo gameData)
        {
            mWorldMap.Clear();
            mWorldMap2.Clear();

            foreach (var e in gameData.WorldMapCountry.Values)
            {
                WorldMapCountry wmr = new WorldMapCountry(e.countryname, e.lv);
                mWorldMap.Add(e.id, wmr);
                mWorldMap2.Add(e.countryname, e.id);

                foreach (var x in gameData.WorldMapCountryPlace.Values)
                {
                    if (e.id == x.country)
                        wmr.placeLst.Add(new WorldMapCountryPlaceInterest(x.placeiconpath, x.placename, x.level, x.id));
                }

                foreach (var x in gameData.WorldMapCountryMonster.Values)
                {
                    if (e.id == x.country)
                        wmr.monLst.Add(new WorldMapCountryMonster(x.archetype));
                }
            }

            foreach (var e in gameData.WorldMapCountryPlace.Values)
            {
                mWorldMapInterestMarkLst.Add(new WorldMapInterestMarkerPos(e.posx, e.posy, e.posz));
            }

            //Create a list 
            List<int> countriesIDList = mWorldMap.Keys.ToList();
            countriesIDList.Sort();
            for (int x = 0; x < countriesIDList.Count; ++x)
            {
                mWorldMapLst.Add(mWorldMap[countriesIDList[x]]);
            }
        }

        public static string GetCountryName(int regionID)
        {
            if (!mWorldMap.ContainsKey(regionID))
                return null;

            return mWorldMap[regionID].name;
        }
        public static string GetCountryLevel(int regionID)
        {
            if (!mWorldMap.ContainsKey(regionID))
                return null;

            return mWorldMap[regionID].lvRange;
        }
        public static string GetCountryLevel(string regionName)
        {
            if (!mWorldMap2.ContainsKey(regionName))
                return null;

            return mWorldMap[mWorldMap2[regionName]].lvRange;
        }

        public static List<WorldMapCountryMonster> GetCountryMonster(int regionID)
        {
            if (!mWorldMap.ContainsKey(regionID))
                return null;

            return mWorldMap[regionID].monLst;
        }
        public static List<WorldMapCountryMonster> GetCountryMonster(string regionName)
        {
            if (!mWorldMap2.ContainsKey(regionName))
                return null;

            return mWorldMap[mWorldMap2[regionName]].monLst;
        }


        public static List<WorldMapCountryPlaceInterest> GetCountryArea(int regionID)
        {
            if (!mWorldMap.ContainsKey(regionID))
                return null;

            return mWorldMap[regionID].placeLst;
        }
        public static List<WorldMapCountryPlaceInterest> GetCountryArea(string regionName)
        {
            if (!mWorldMap2.ContainsKey(regionName))
                return null;

            return mWorldMap[mWorldMap2[regionName]].placeLst;
        }
    }
}