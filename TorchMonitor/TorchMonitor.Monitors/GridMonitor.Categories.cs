using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using TorchUtils;

namespace TorchMonitor.Monitors
{
    public sealed partial class GridMonitor
    {
        static readonly Type[] _functionalBlockCategory =
        {
            typeof(IMyFunctionalBlock),
        };

        static readonly Type[] _conveyorCategory =
        {
            typeof(IMyConveyor),
            typeof(IMyConveyorTube),
        };

        static readonly Type[] _sorterCategory =
        {
            typeof(IMyConveyorSorter),
        };

        static readonly Type[] _subpartCategory =
        {
            typeof(IMyMechanicalConnectionBlock),
            typeof(IMyShipConnector),
        };

        static readonly Type[] _pbCategory =
        {
            typeof(IMyProgrammableBlock),
        };

        static readonly Type[] _productionBlockCategory =
        {
            typeof(IMyProductionBlock),
        };

        static readonly Type[] _shipToolCategory =
        {
            typeof(IMyShipToolBase),
        };

        static void CountCategories(object block, IDictionary<string, int> counts)
        {
            IncrementIfOfAny(block, counts, _functionalBlockCategory, "functional");
            IncrementIfOfAny(block, counts, _conveyorCategory, "conveyor");
            IncrementIfOfAny(block, counts, _sorterCategory, "conveyor_sorter");
            IncrementIfOfAny(block, counts, _subpartCategory, "subpart");
            IncrementIfOfAny(block, counts, _pbCategory, "pb");
            IncrementIfOfAny(block, counts, _productionBlockCategory, "production");
            IncrementIfOfAny(block, counts, _shipToolCategory, "ship_tool");
        }

        static void IncrementIfOfAny(object block, IDictionary<string, int> counts, IEnumerable<Type> category, string name)
        {
            foreach (var type in category)
            {
                if (type.IsInstanceOfType(block))
                {
                    counts.Increment(name);
                    return;
                }
            }
        }
    }
}