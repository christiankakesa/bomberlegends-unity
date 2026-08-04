using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace BomberLegends.Tests.EditMode.Architecture
{
    /// <summary>
    /// Enforces the assembly dependency rules defined in docs/03-ARCHITECTURE.md §2.
    /// These rules are the structural guarantees the rest of the project relies on:
    /// an engine-free simulation, a presentation layer that cannot reach into gameplay,
    /// and an acyclic graph. A violation here is an architecture regression, not a style issue.
    /// </summary>
    public sealed class AssemblyGraphTests
    {
        private const string Prefix = "BomberLegends.";

        private static readonly string[] EngineFreeAssemblies =
        {
            "BomberLegends.Core",
            "BomberLegends.Simulation"
        };

        private static readonly string[] RuntimeAssemblies =
        {
            "BomberLegends.Core",
            "BomberLegends.Simulation",
            "BomberLegends.Data",
            "BomberLegends.Services",
            "BomberLegends.Input",
            "BomberLegends.Meta",
            "BomberLegends.Gameplay",
            "BomberLegends.UI",
            "BomberLegends.Bootstrap"
        };

        [TestCaseSource(nameof(EngineFreeAssemblies))]
        public void EngineFreeAssembly_DoesNotReferenceUnity(string assemblyName)
        {
            var offenders = ReferencesOf(assemblyName)
                .Where(r => r.StartsWith("UnityEngine") || r.StartsWith("UnityEditor"))
                .ToArray();

            Assert.That(offenders, Is.Empty,
                $"{assemblyName} must stay engine-free (decision D1). It references: {string.Join(", ", offenders)}");
        }

        [Test]
        public void Ui_DoesNotReferenceGameplay()
        {
            Assert.That(ReferencesOf("BomberLegends.UI"), Does.Not.Contain("BomberLegends.Gameplay"),
                "UI must not reach into Gameplay. Cross-feature communication goes through Data event channels.");
        }

        [Test]
        public void Gameplay_DoesNotReferenceUi()
        {
            Assert.That(ReferencesOf("BomberLegends.Gameplay"), Does.Not.Contain("BomberLegends.UI"),
                "Gameplay must not reach into UI. Cross-feature communication goes through Data event channels.");
        }

        [Test]
        public void Simulation_DoesNotReferenceData()
        {
            Assert.That(ReferencesOf("BomberLegends.Simulation"), Does.Not.Contain("BomberLegends.Data"),
                "Configuration is baked into plain structs by Data and passed in. Simulation must not read ScriptableObjects.");
        }

        [Test]
        public void NothingReferencesBootstrap()
        {
            foreach (var assembly in RuntimeAssemblies.Where(a => a != "BomberLegends.Bootstrap"))
            {
                Assert.That(ReferencesOf(assembly), Does.Not.Contain("BomberLegends.Bootstrap"),
                    $"{assembly} references Bootstrap. Bootstrap is the composition root and must remain a leaf.");
            }
        }

        [Test]
        public void DependencyGraph_IsAcyclic()
        {
            var graph = RuntimeAssemblies.ToDictionary(
                name => name,
                name => ReferencesOf(name).Where(r => r.StartsWith(Prefix)).ToArray());

            var visiting = new HashSet<string>();
            var visited = new HashSet<string>();

            foreach (var node in graph.Keys)
            {
                var cycle = FindCycle(node, graph, visiting, visited, new List<string>());
                Assert.That(cycle, Is.Null, $"Dependency cycle detected: {cycle}");
            }
        }

        private static string? FindCycle(
            string node,
            IReadOnlyDictionary<string, string[]> graph,
            ISet<string> visiting,
            ISet<string> visited,
            List<string> path)
        {
            if (visited.Contains(node))
            {
                return null;
            }

            if (!visiting.Add(node))
            {
                return string.Join(" -> ", path) + " -> " + node;
            }

            path.Add(node);

            if (graph.TryGetValue(node, out var dependencies))
            {
                foreach (var dependency in dependencies)
                {
                    var cycle = FindCycle(dependency, graph, visiting, visited, path);
                    if (cycle != null)
                    {
                        return cycle;
                    }
                }
            }

            path.RemoveAt(path.Count - 1);
            visiting.Remove(node);
            visited.Add(node);
            return null;
        }

        private static string[] ReferencesOf(string assemblyName)
        {
            var assembly = Assembly.Load(assemblyName);
            Assert.That(assembly, Is.Not.Null, $"Assembly {assemblyName} was not found. Did an asmdef get renamed?");

            return assembly.GetReferencedAssemblies()
                .Select(reference => reference.Name)
                .Where(name => name != null)
                .Select(name => name!)
                .ToArray();
        }
    }
}
