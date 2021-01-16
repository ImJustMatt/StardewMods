﻿using System;
using System.Collections.Generic;
using Harmony;

namespace Common.HarmonyPatches
{
    public class PatternPatch
    {
        private enum PatchType
        {
            Replace,
            Prepend
        };
        public string Text { get; private set; }
        public int Skipped { get; private set; }
        public bool Loop => _patchType == PatchType.Replace && _loop == -1 || --_loop > 0;

        private readonly List<CodeInstruction> _patterns = new List<CodeInstruction>();
        private readonly Queue<int> _patternIndex = new Queue<int>();
        private Action<LinkedList<CodeInstruction>> _patch;
        private readonly PatchType _patchType;
        private int _startIndex = 0;
        private int _endIndex = 0;
        private int _index;
        private int _loop;
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

        public PatternPatch Find(params CodeInstruction[] pattern)
        {
            _patterns.AddRange(pattern);
            _patternIndex.Enqueue(_patterns.Count);
            return this;
        }
        public PatternPatch Patch(Action<LinkedList<CodeInstruction>> patch)
        {
            _patch = patch;
            return this;
        }
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

        public PatternPatch Skip(int skip)
        {
            Skipped = skip;
            return this;
        }

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
            _patch?.Invoke(rawStack);
        }
    }
}