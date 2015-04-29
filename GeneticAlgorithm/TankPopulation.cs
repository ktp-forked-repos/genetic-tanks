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
    private static readonly int NumClones = Settings.Default.NumClones;

    private static readonly float ReplacementPercent =
      Settings.Default.ReplacementPercent;

    private static readonly float MutationRate =
      Settings.Default.MutationRate;

    private readonly int m_size;
    private readonly Dictionary<int, TankGenome> m_genomes = 
      new Dictionary<int, TankGenome>();
    
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

    public int Size { get { return m_size; } }

    public int Generation { get; private set; }

    public IEnumerable<TankGenome> Genomes { get { return m_genomes.Values; } }

    public TankGenome GetGenome(int id)
    {
      TankGenome result;
      return TryGetGenome(id, out result) ? result : null;
    }

    public bool TryGetGenome(int id, out TankGenome genome)
    {
      return m_genomes.TryGetValue(id, out genome);
    }

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
