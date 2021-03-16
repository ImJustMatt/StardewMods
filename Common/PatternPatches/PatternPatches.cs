using System.Collections;
using System.Collections.Generic;
using Harmony;
using StardewModdingAPI;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace ImJustMatt.Common.PatternPatches
{
    internal class PatternPatches : IEnumerable<CodeInstruction>
    {
        private static IMonitor _monitor;
        private readonly IEnumerable<CodeInstruction> _instructions;
        private readonly Queue<PatternPatch> _patternPatches = new();

        public PatternPatches(IEnumerable<CodeInstruction> instructions, IMonitor monitor)
        {
            _instructions = instructions;
            _monitor = monitor;
        }

        public bool Done => _patternPatches.Count == 0;

        public IEnumerator<CodeInstruction> GetEnumerator()
        {
            var currentOperation = _patternPatches.Dequeue();
            var rawStack = new LinkedList<CodeInstruction>();
            var skipped = 0;
            var done = false;

            foreach (var instruction in _instructions)
            {
                // Skipped instructions
                if (skipped > 0)
                {
                    skipped--;
                    continue;
                }

                // Pattern does not match or done matching patterns
                if (done || !currentOperation.Matches(instruction))
                {
                    rawStack.AddLast(instruction);
                    continue;
                }

                // Return patched code
                if (currentOperation.Text != null)
                    _monitor.LogOnce(currentOperation.Text);
                rawStack.AddLast(instruction);
                currentOperation.Patches(rawStack);
                foreach (var patch in rawStack) yield return patch;

                rawStack.Clear();
                skipped = currentOperation.Skipped;

                // Repeat
                if (currentOperation.Loop)
                    continue;

                // Next pattern
                if (_patternPatches.Count > 0)
                    currentOperation = _patternPatches.Dequeue();
                else
                    done = true;
            }

            foreach (var instruction in rawStack) yield return instruction;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public PatternPatch Find(params CodeInstruction[] pattern)
        {
            var operation = new PatternPatch(pattern);
            _patternPatches.Enqueue(operation);
            return operation;
        }
    }
}