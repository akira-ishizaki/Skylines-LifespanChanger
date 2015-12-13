using ColossalFramework;
using LifespanChanger;
using System;

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
    private void FinishSchoolOrWork(uint citizenID, ref Citizen data)
    {
    }

    private void Die(uint citizenID, ref Citizen data)
    {
    }
}