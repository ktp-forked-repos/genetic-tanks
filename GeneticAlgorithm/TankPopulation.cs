using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GeneticTanks.Properties;
using log4net;

namespace GeneticTanks.GeneticAlgorithm
{
  sealed class TankPopulation
  {
    private static readonly ILog Log = LogManager.GetLogger(
      MethodBase.GetCurrentMethod().DeclaringType);

    private static readonly Random Random = new Random();
    public static readonly int NumClones = Settings.Default.NumClones;

    private readonly int m_size;
    private float m_mutationRate = 0.25f;
    private float m_replacementPercent = 0.5f;
    private readonly Dictionary<int, TankGenome> m_genomes = 
      new Dictionary<int, TankGenome>();
    
    /// <summary>
    /// Create the population in a randomized state.
    /// </summary>
    /// <param name="size"></param>
    public TankPopulation(int size)
    {
      if (size <= 0)
      {
        throw new ArgumentOutOfRangeException("size");
      }

      m_size = size;
      Generation = 1;
      
      for (var i = 0; i < Size; i++)
      {
        var genome = new TankGenome { Id = i };
        genome.Randomize();
        m_genomes[i] = genome;
      }
    }
    
    /// <summary>
    /// The population size.
    /// </summary>
    public int Size { get { return m_size; } }

    /// <summary>
    /// The counter for the current generation.
    /// </summary>
    public int Generation { get; private set; }

    /// <summary>
    /// All of the genomes in the current population.
    /// </summary>
    public IEnumerable<TankGenome> Genomes { get { return m_genomes.Values; } }

    /// <summary>
    /// Determines how many of the worst performing members of the population 
    /// will be removed and replaced when a new generation is generated.  Value 
    /// between 0 and 1.
    /// </summary>
    public float ReplacementPercent
    {
      get { return m_replacementPercent; }
      set
      {
        value = Math.Min(value, 1f);
        value = Math.Max(value, 0f);
        m_replacementPercent = value;
      }
    }

    /// <summary>
    /// The percent chance that a newly generated genome will experience a 
    /// mutation.  Value between 0 and 1.
    /// </summary>
    public float MutationRate
    {
      get { return m_mutationRate; }
      set
      {
        value = Math.Min(value, 1f);
        value = Math.Max(value, 0f);
        m_mutationRate = value;
      }
    }

    /// <summary>
    /// Get a genome from the population.
    /// </summary>
    /// <param name="id"></param>
    /// <returns>
    /// The genome, or null if it was not found.
    /// </returns>
    public TankGenome GetGenome(int id)
    {
      TankGenome result;
      return TryGetGenome(id, out result) ? result : null;
    }

    /// <summary>
    /// Attempts to get a genome from the population.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="genome"></param>
    /// <returns>
    /// True if the genome was retrieved.
    /// </returns>
    public bool TryGetGenome(int id, out TankGenome genome)
    {
      return m_genomes.TryGetValue(id, out genome);
    }

    /// <summary>
    /// Generates the next generation of the population.
    /// </summary>
    public void NextGeneration()
    {
      var avgDamage = Genomes.Average(g => g.DamageDealt);
      var avgSurvivalTime = Genomes.Average(g => g.SurvivalTime);

      // make a copy of the genomes and sort by fitness (high to low)
      var orderedGenomes = Genomes.ToList();
      orderedGenomes.Sort((a, b) =>
        {
          var aFitness = a.GetFitness(avgDamage, avgSurvivalTime);
          var bFitness = b.GetFitness(avgDamage, avgSurvivalTime);

          if (aFitness > bFitness)
          {
            return 1;
          }
          if (aFitness < bFitness)
          {
            return -1;
          }

          return 0;
        });

      // trim out the worst genomes
      var toRemove = (int)Math.Floor(ReplacementPercent * Size);
      orderedGenomes.RemoveRange(Size - toRemove, toRemove);

      for (var i = 0; i < Size; i++)
      {
        if (i < NumClones)
        {
          m_genomes[i] = orderedGenomes[i];
          orderedGenomes[i].Id = i;
          orderedGenomes[i].GenomeType = GenomeType.Clone;
          orderedGenomes[i].ResetStats();
        }
        else
        {
          var genome = DoCrossover(orderedGenomes);
          genome.Id = i;
          
          if (Random.NextDouble() < MutationRate)
          {
            genome.Mutate();
          }

          m_genomes[i] = genome;
        }
      }

      Generation++;
    }

    // select two random genomes from the list and cross them to make a child
    private static TankGenome DoCrossover(IReadOnlyList<TankGenome> genomes)
    {
      var a = genomes[Random.Next(genomes.Count)];
      var b = a;

      while (a == b)
      {
        b = genomes[Random.Next(genomes.Count)];
      }

      return TankGenome.CrossOver(a, b);
    }
  }
}
