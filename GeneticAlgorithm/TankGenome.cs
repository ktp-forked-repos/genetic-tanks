using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using log4net;

namespace GeneticTanks.GeneticAlgorithm
{
  enum Attribute
  {
    Health,
    Size,
    Speed,
    TurnSpeed,
    TurretRotationSpeed,
    TurretRangeOfMotion,

    SensorRange,
    GunRange,
    GunDamage,
    GunSpeed,
    GunReloadTime
  }

  enum GenomeType
  {
    Random,
    Clone,
    CrossOver
  }

  sealed class TankGenome
  {
    private static readonly ILog Log = LogManager.GetLogger(
      MethodBase.GetCurrentMethod().DeclaringType);

    /// <summary>
    /// All the attributes, for convenience.
    /// </summary>
    public static readonly Attribute[] Attributes =
      (Attribute[])Enum.GetValues(typeof(Attribute));
    
    /// <summary>
    /// The max value for any single attribute.
    /// </summary>
    public static readonly int MaxAttributeValue = 
      Properties.Settings.Default.TankMaxAttributeValue;
    
    /// <summary>
    /// The total number of attribute points distributed in the genome.
    /// </summary>
    public static readonly int TotalAttributePoints =
      Properties.Settings.Default.TankPointsPerAttribute * Attributes.Length;
    
    private static readonly Random Random = new Random();

    /// <summary>
    /// Create a new genome by combining two genomes.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static TankGenome CrossOver(TankGenome a, TankGenome b)
    {
      if (a == null || b == null)
      {
        throw new ArgumentNullException(a == null ? "a" : "b");
      }

      var result = new TankGenome();
      var points = TotalAttributePoints;

      // populate the attributes with the minimum value from each parent
      foreach (var attribute in Attributes)
      {
        var value = Math.Min(a.GetAttribute(attribute),
          b.GetAttribute(attribute));
        result.m_attributes[attribute] = value;
        points -= value;
      }

      // distribute the remaining points
      while (points > 0)
      {
        result.m_attributes[result.GetRandomAttribute(false, true, null)]++;
        points--;
      }

      result.GenomeType = GenomeType.CrossOver;
      Debug.Assert(result.Validate());
      return result;
    }

    private readonly Dictionary<Attribute, int> m_attributes = 
      new Dictionary<Attribute, int>();

    /// <summary>
    /// Create the genome.
    /// </summary>
    public TankGenome()
    {
      Clear();
    }

    /// <summary>
    /// Identifier for this genome.
    /// </summary>
    public int Id { get; set; }

    public GenomeType GenomeType { get; set; }

    /// <summary>
    /// Tracks the damage done by this genome's tank.
    /// </summary>
    public float DamageDealt { get; set; }

    /// <summary>
    /// Tracks the time in seconds this tank has survived.
    /// </summary>
    public float SurvivalTime { get; set; }

    /// <summary>
    /// Tracks the number of kills this genome's tank has achieved.
    /// </summary>
    public int NumKills { get; set; }

    /// <summary>
    /// Calculate the fitness of this tank by comparing its stats to the 
    /// population average.
    /// </summary>
    /// <param name="avgDamageDealt"></param>
    /// <param name="avgSurvivalTime"></param>
    /// <returns></returns>
    public float GetFitness(float avgDamageDealt, float avgSurvivalTime )
    {
      var result = DamageDealt / avgDamageDealt;
      result += SurvivalTime / avgSurvivalTime;
      result += NumKills;
      return result;
    }

    /// <summary>
    /// Reset all the stats tracked in the genome.
    /// </summary>
    public void ResetStats()
    {
      DamageDealt = 0f;
      SurvivalTime = 0f;
      NumKills = 0;
    }

    /// <summary>
    /// Get the score of an attribute.
    /// </summary>
    /// <param name="attribute"></param>
    /// <returns></returns>
    public int GetAttribute(Attribute attribute)
    {
      return m_attributes[attribute];
    }

    /// <summary>
    /// Resets all attributes back to 0.
    /// </summary>
    public void Clear()
    {
      foreach (var attribute in Attributes)
      {
        m_attributes[attribute] = 0;
      }
    }

    /// <summary>
    /// Clears the attributes and randomizes them.
    /// </summary>
    public void Randomize()
    {
      Clear();

      var points = TotalAttributePoints;
      while (points > 0)
      {
        m_attributes[GetRandomAttribute(false, true, null)]++;
        points--;
      }

      GenomeType = GenomeType.Random;
      Debug.Assert(Validate());
    }

    /// <summary>
    /// Pulls a point from one random attribute and assigns it to another 
    /// random attribute.
    /// </summary>
    public void Mutate()
    {
      var source = GetRandomAttribute(true, false, null);
      var target = GetRandomAttribute(false, true, source);

      m_attributes[source]--;
      m_attributes[target]++;

      Debug.Assert(Validate());
    }

    /// <summary>
    /// Returns true if all the attributes in this genome are valid.
    /// </summary>
    /// <returns></returns>
    public bool Validate()
    {

      var badCount = m_attributes.Values
        .Count(v => v < 0 || v > MaxAttributeValue);
      return badCount == 0 && m_attributes.Count == Attributes.Length;
    }

    #region Private Methods

    // gets a random attribute that excludes attributes at 0 and/or max, and/or
    // the specified attribute
    private Attribute GetRandomAttribute(bool excludeZero, bool excludeMax,
      Attribute? excludeValue)
    {
      var result = Attributes.First();
      var valid = false;

      while (!valid)
      {
        result = Attributes[Random.Next(Attributes.Length)];
        valid = true;

        if (excludeZero)
        {
          valid = m_attributes[result] != 0;
        }
        if (excludeMax)
        {
          valid = valid && m_attributes[result] != MaxAttributeValue;
        }
        if (excludeValue != null)
        {
          valid = valid && result != excludeValue;
        }
      }

      return result;
    }

    #endregion
  }
}
