using System;
using System.Collections.Generic;
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
    public const int MaxAttributeValue = 5;
    
    /// <summary>
    /// The total number of attribute points distributed in the genome.
    /// </summary>
    public static readonly int TotalAttributePoints =
      2 * Attributes.Length;
    
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
        result.m_attributes[result.GetRandomAtribute()]++;
        points--;
      }

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
    
    public float DamageDealt { get; set; }
    public float SurvivalTime { get; set; }
    public int NumKills { get; set; }

    public float GetFitness(float avgDamageDealt, float avgSurvivalTime )
    {
      var result = DamageDealt / avgDamageDealt;
      result += SurvivalTime / avgSurvivalTime;
      result += NumKills;
      return result;
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
    /// Clears the attributes and 
    /// </summary>
    public void Randomize()
    {
      int points = TotalAttributePoints;
      while (points > 0)
      {
        m_attributes[GetRandomAtribute()]++;
        points--;
      }
    }

    /// <summary>
    /// Pulls a point from one random attribute and assigns it to another 
    /// random attribute.
    /// </summary>
    public void Mutate()
    {
      var source = GetRandomAtribute();
      Attribute target;

      do
      {
        target = GetRandomAtribute();
      } while (source == target);

      m_attributes[source]--;
      m_attributes[target]++;
    }

    #region Private Methods

    // gets a random attribute that is not at max value
    private Attribute GetRandomAtribute()
    {
      Attribute result;

      do
      {
        result = Attributes[Random.Next(Attributes.Length)];
      } while (m_attributes[result] == MaxAttributeValue);

      return result;
    }

    #endregion
  }
}
