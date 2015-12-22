using ColossalFramework;
using ColossalFramework.Steamworks;
using ColossalFramework.Threading;
using LifespanChanger;
using System;
using UnityEngine;

public class CustomResidentAI : HumanAI
{
    private bool UpdateAge(uint citizenID, ref Citizen data)
    {
        int age = data.Age + 1;
        if (age <= Citizen.AGE_LIMIT_TEEN)
        {
            if (age == Citizen.AGE_LIMIT_CHILD || age == Citizen.AGE_LIMIT_TEEN)
            {
                this.FinishSchoolOrWork(citizenID, ref data);
            }
        }
        else if (age == Citizen.AGE_LIMIT_YOUNG || age == Citizen.AGE_LIMIT_ADULT)
        {
            this.FinishSchoolOrWork(citizenID, ref data);
        }
        else if ((data.m_flags & Citizen.Flags.Student) != Citizen.Flags.None && age % 15 == 0)
        {
            this.FinishSchoolOrWork(citizenID, ref data);
        }
        if ((data.m_flags & Citizen.Flags.Original) != Citizen.Flags.None)
        {
            CitizenManager instance = Singleton<CitizenManager>.instance;
            if (instance.m_tempOldestOriginalResident < age)
            {
                instance.m_tempOldestOriginalResident = age;
            }
            if (age == Citizen.AGE_LIMIT_SENIOR)
            {
                Singleton<StatisticsManager>.instance.Acquire<StatisticInt32>(StatisticType.FullLifespans).Add(1);
            }
        }
        data.Age = age;

        bool dieFlag = false;
        switch(ModMain.ModConf.LifespanValue)
        {
            case ModMain.SENIOR_AGE:
                if (age >= Citizen.AGE_LIMIT_ADULT && data.CurrentLocation != Citizen.Location.Moving && data.m_vehicle == 0)
                {
                    dieFlag = true;
                }
                break;
            case ModMain.MAX_AGE:
                if (age >= Citizen.AGE_LIMIT_FINAL && data.CurrentLocation != Citizen.Location.Moving && data.m_vehicle == 0)
                {
                    dieFlag = true;
                }
                break;
            case ModMain.MORE_RANDOM_AGE:
                if (age >= Citizen.AGE_LIMIT_ADULT && data.CurrentLocation != Citizen.Location.Moving && data.m_vehicle == 0
                    && Singleton<SimulationManager>.instance.m_randomizer.Int32(Citizen.AGE_LIMIT_ADULT, Citizen.AGE_LIMIT_FINAL) <= age)
                {
                    dieFlag = true;
                }
                break;
            case ModMain.VERY_RANDOM_AGE:
                if (age >= Citizen.AGE_LIMIT_ADULT && data.CurrentLocation != Citizen.Location.Moving && data.m_vehicle == 0
                    && Singleton<SimulationManager>.instance.m_randomizer.Int32(0, Math.Max(0, Citizen.AGE_LIMIT_FINAL - age)) == 0)
                {
                    dieFlag = true;
                }
                break;
            default:
                if (age >= Citizen.AGE_LIMIT_SENIOR && data.CurrentLocation != Citizen.Location.Moving && data.m_vehicle == 0
                    && Singleton<SimulationManager>.instance.m_randomizer.Int32(Citizen.AGE_LIMIT_SENIOR, Citizen.AGE_LIMIT_FINAL) <= age)
                {
                    dieFlag = true;
                }
                break;
        }
        if(dieFlag)
        {
            this.Die(citizenID, ref data);
            if (Singleton<SimulationManager>.instance.m_randomizer.Int32(2u) == 0)
            {
                Singleton<CitizenManager>.instance.ReleaseCitizen(citizenID);
                return true;
            }
        }
        return false;
    }

    private void Die(uint citizenID, ref Citizen data)
    {
        data.Sick = false;
        data.Dead = true;
        data.SetParkedVehicle(citizenID, 0);
        if ((data.m_flags & Citizen.Flags.MovingIn) == Citizen.Flags.None)
        {
            ushort buildingByLocation = data.GetBuildingByLocation();
            if (buildingByLocation == 0)
            {
                buildingByLocation = data.m_homeBuilding;
            }
            if (buildingByLocation != 0)
            {
                DistrictManager mTempCount = Singleton<DistrictManager>.instance;
                Vector3 mPosition = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingByLocation].m_position;
                byte district = mTempCount.GetDistrict(mPosition);
                mTempCount.m_districts.m_buffer[district].m_deathData.m_tempCount = mTempCount.m_districts.m_buffer[district].m_deathData.m_tempCount + 1;
            }
        }
    }

    private void FinishSchoolOrWork(uint citizenID, ref Citizen data)
    {
        if (data.m_workBuilding != 0)
        {
            if (data.CurrentLocation == Citizen.Location.Work && data.m_homeBuilding != 0)
            {
                base.StartMoving(citizenID, ref data, data.m_workBuilding, data.m_homeBuilding);
            }
            BuildingManager instance = Singleton<BuildingManager>.instance;
            CitizenManager instance2 = Singleton<CitizenManager>.instance;
            uint num = instance.m_buildings.m_buffer[(int)data.m_workBuilding].m_citizenUnits;
            int num2 = 0;
            while (num != 0u)
            {
                uint nextUnit = instance2.m_units.m_buffer[(int)((UIntPtr)num)].m_nextUnit;
                CitizenUnit.Flags flags = instance2.m_units.m_buffer[(int)((UIntPtr)num)].m_flags;
                if ((ushort)(flags & (CitizenUnit.Flags.Work | CitizenUnit.Flags.Student)) != 0)
                {
                    if ((ushort)(flags & CitizenUnit.Flags.Student) != 0)
                    {
                        if (data.RemoveFromUnit(citizenID, ref instance2.m_units.m_buffer[(int)((UIntPtr)num)]))
                        {
                            BuildingInfo info = instance.m_buildings.m_buffer[(int)data.m_workBuilding].Info;
                            if (info.m_buildingAI.GetEducationLevel1())
                            {
                                data.Education1 = true;
                            }
                            if (info.m_buildingAI.GetEducationLevel2())
                            {
                                data.Education2 = true;
                            }
                            if (info.m_buildingAI.GetEducationLevel3())
                            {
                                data.Education3 = true;
                            }
                            data.m_workBuilding = 0;
                            data.m_flags &= ~Citizen.Flags.Student;
                            if ((data.m_flags & Citizen.Flags.Original) != Citizen.Flags.None && data.EducationLevel == Citizen.Education.ThreeSchools && instance2.m_fullyEducatedOriginalResidents++ == 0 && Singleton<SimulationManager>.instance.m_metaData.m_disableAchievements != SimulationMetaData.MetaBool.True)
                            {
                                ThreadHelper.dispatcher.Dispatch(delegate
                                {
                                    if (!Steam.achievements["ClimbingTheSocialLadder"].achieved)
                                    {
                                        Steam.achievements["ClimbingTheSocialLadder"].Unlock();
                                    }
                                });
                            }
                            return;
                        }
                    }
                    else if (data.RemoveFromUnit(citizenID, ref instance2.m_units.m_buffer[(int)((UIntPtr)num)]))
                    {
                        data.m_workBuilding = 0;
                        data.m_flags &= ~Citizen.Flags.Student;
                        return;
                    }
                }
                num = nextUnit;
                if (++num2 > 524288)
                {
                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                    break;
                }
            }
        }
    }
}