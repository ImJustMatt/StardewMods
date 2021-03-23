using System;
using System.Collections.Generic;
using Harmony;

// ReSharper disable UnusedMember.Global

namespace ImJustMatt.Common.Patches
{
    internal class PatternPatch
    {
        private readonly IList<Action<LinkedList<CodeInstruction>>> _patches = new List<Action<LinkedList<CodeInstruction>>>();
        private readonly PatchType _patchType;
        private readonly Queue<int> _patternIndex = new();

        private readonly List<CodeInstruction> _patterns = new();
        private int _endIndex;
        private int _index;
        private int _loop;
        private int _startIndex;

        public PatternPatch(ICollection<CodeInstruction> pattern)
        {
            if (pattern == null || pattern.Count == 0)
            {
                _patchType = PatchType.Prepend;
                return;
            }

            _patchType = PatchType.Replace;
            _patterns.AddRange(pattern);
            _patternIndex.Enqueue(_patterns.Count);
        }

        public string Text { get; private set; }
        public int Skipped { get; private set; }
        public bool Loop => _patchType == PatchType.Replace && _loop == -1 || --_loop > 0;

        public PatternPatch Find(params CodeInstruction[] pattern)
        {
            _patterns.AddRange(pattern);
            _patternIndex.Enqueue(_patterns.Count);
            return this;
        }

        public PatternPatch Patch(Action<LinkedList<CodeInstruction>> patch)
        {
            _patches.Add(patch);
            return this;
        }

        // ReSharper disable once UnusedMethodReturnValue.Global
        public PatternPatch Patch(params CodeInstruction[] patches)
        {
            _patterns.AddRange(patches);
            return this;
        }

        public PatternPatch Log(string text)
        {
            Text = text;
            return this;
        }

        // ReSharper disable once UnusedMethodReturnValue.Global
        public PatternPatch Skip(int skip)
        {
            Skipped = skip;
            return this;
        }

        // ReSharper disable once UnusedMethodReturnValue.Global
        public PatternPatch Repeat(int loop)
        {
            _loop = loop;
            return this;
        }

        public bool Matches(CodeInstruction instruction)
        {
            // Return true if no pattern to match
            if (_patchType == PatchType.Prepend)
                return true;

            // Initialize end index
            if (_startIndex == _endIndex)
                _endIndex = _patternIndex.Dequeue();

            // Reset on loop
            if (_index == _endIndex)
                _index = _startIndex;

            // Opcode not matching
            if (!_patterns[_index].opcode.Equals(instruction.opcode))
            {
                _index = _startIndex;
                return false;
            }

            // Operand not matching
            if (_patterns[_index].operand != null && !_patterns[_index].operand.Equals(instruction.operand))
            {
                _index = _startIndex;
                return false;
            }

            // Incomplete pattern search
            if (++_index != _endIndex)
                return false;

            // Complete pattern search
            if (_patternIndex.Count <= 0)
                return true;

            // Incomplete pattern search
            _startIndex = _endIndex;
            return false;
        }

        public void Patches(LinkedList<CodeInstruction> rawStack)
        {
            foreach (var patch in _patches) patch?.Invoke(rawStack);
        }

        private enum PatchType
        {
            Replace,
            Prepend
        }
    }
}