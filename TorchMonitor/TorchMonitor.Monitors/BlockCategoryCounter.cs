using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Utils.General;

namespace TorchMonitor.Monitors
{
    public sealed class BlockCategoryCounter
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

        readonly ConcurrentDictionary<string, int> _counts;

        public BlockCategoryCounter()
        {
            _counts = new ConcurrentDictionary<string, int>();
        }

        public bool Any() => _counts.Any();

        public IEnumerable<KeyValuePair<string, int>> Counts => _counts;

        // ReSharper disable once SuggestBaseTypeForParameter
        public void Count(MyCubeBlock block)
        {
            Count(block, _functionalBlockCategory, "functional");
            Count(block, _conveyorCategory, "conveyor");
            Count(block, _sorterCategory, "conveyor_sorter");
            Count(block, _subpartCategory, "subpart");
            Count(block, _pbCategory, "pb");
            Count(block, _productionBlockCategory, "production");
            Count(block, _shipToolCategory, "ship_tool");
        }

        void Count(object block, IEnumerable<Type> category, string name)
        {
            foreach (var type in category)
            {
                if (type.IsInstanceOfType(block))
                {
                    _counts.Increment(name);
                    return;
                }
            }
        }
    }
}