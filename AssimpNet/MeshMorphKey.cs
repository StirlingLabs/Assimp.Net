/*
* Copyright (c) 2012-2018 AssimpNet - Nicholas Woodfield
* 
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
* 
* The above copyright notice and this permission notice shall be included in
* all copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
* THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using Assimp.Unmanaged;

namespace Assimp
{
    /// <summary>
    /// Binds a morph animation mesh to a specific point in time.
    /// </summary>
    public sealed class MeshMorphKey : IMarshalable<MeshMorphKey, AiMeshMorphKey>
    {
        private double m_time;
        private List<int> m_values;
        private List<double> m_weights;

        /// <summary>
        /// Gets or sets the time of this keyframe.
        /// </summary>
        public double Time
        {
            get
            {
                return m_time;
            }
            set
            {
                m_time = value;
            }
        }

        /// <summary>
        /// Gets the values at the time of this keyframe.
        /// </summary>
        public List<int> Values
        {
            get
            {
                return m_values;
            }
        }

        /// <summary>
        /// Gets the weights at the time of this keyframe.
        /// </summary>
        public List<double> Weights
        {
            get
            {
                return m_weights;
            }
        }

        /// <summary>
        /// Constructs a new instance of the <see cref="MeshMorphKey"/> class.
        /// </summary>
        public MeshMorphKey()
        {
            m_time = 0.0;
            m_values = new List<int>();
            m_weights = new List<double>();
        }

        #region IMarshalable Implementation

        /// <summary>
        /// Gets if the native value type is blittable (that is, does not require marshaling by the runtime, e.g. has MarshalAs attributes).
        /// </summary>
        bool IMarshalable<MeshMorphKey, AiMeshMorphKey>.IsNativeBlittable { get { return true; } }

        /// <summary>
        /// Writes the managed data to the native value.
        /// </summary>
        /// <param name="thisPtr">Optional pointer to the memory that will hold the native value.</param>
        /// <param name="nativeValue">Output native value</param>
        void IMarshalable<MeshMorphKey, AiMeshMorphKey>.ToNative(IntPtr thisPtr, out AiMeshMorphKey nativeValue)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Reads the unmanaged data from the native value.
        /// </summary>
        /// <param name="nativeValue">Input native value</param>
        void IMarshalable<MeshMorphKey, AiMeshMorphKey>.FromNative(in AiMeshMorphKey nativeValue)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Frees unmanaged memory created by <see cref="IMarshalable{MeshMorphKey, AiMeshMorphKey}.ToNative"/>.
        /// </summary>
        /// <param name="nativeValue">Native value to free</param>
        /// <param name="freeNative">True if the unmanaged memory should be freed, false otherwise.</param>
        public static void FreeNative(IntPtr nativeValue, bool freeNative)
        {
            if(nativeValue == IntPtr.Zero)
                return;

            AiMeshMorphKey aiMeshMorphKey = MemoryHelper.Read<AiMeshMorphKey>(nativeValue);

            if(aiMeshMorphKey.NumValuesAndWeights > 0)
            {
                if(aiMeshMorphKey.Values != IntPtr.Zero)
                    MemoryHelper.FreeMemory(aiMeshMorphKey.Values);

                if(aiMeshMorphKey.Weights != IntPtr.Zero)
                    MemoryHelper.FreeMemory(aiMeshMorphKey.Weights);
            }

            if(freeNative)
                MemoryHelper.FreeMemory(nativeValue);
        }

        #endregion
    }
}