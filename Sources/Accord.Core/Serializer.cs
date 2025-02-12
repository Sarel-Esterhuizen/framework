﻿// Accord Core Library
// The Accord.NET Framework
// http://accord-framework.net
//
// Copyright © César Souza, 2009-2015
// cesarsouza at gmail.com
//
//    This library is free software; you can redistribute it and/or
//    modify it under the terms of the GNU Lesser General Public
//    License as published by the Free Software Foundation; either
//    version 2.1 of the License, or (at your option) any later version.
//
//    This library is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//    Lesser General Public License for more details.
//
//    You should have received a copy of the GNU Lesser General Public
//    License along with this library; if not, write to the Free Software
//    Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
//

namespace Accord.IO
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    ///   Model serializer. Can be used to serialize and deserialize
    ///   (i.e. save and load) models from the framework to and from
    ///   the disk and other streams.
    /// </summary>
    /// 
    /// <remarks>
    ///   This class uses a binding mechanism to automatically convert
    ///   files saved using older versions of the framework to the new
    ///   format. If a deserialization doesn't work, please fill in a
    ///   bug report at https://github.com/accord-net/framework/issues
    /// </remarks>
    /// 
    public static class Serializer
    {
        private static readonly Object lockObj = new Object();

        /// <summary>
        ///   Saves an object to a stream.
        /// </summary>
        /// 
        /// <param name="obj">The object to be serialized.</param>
        /// <param name="stream">The stream to which the object is to be serialized.</param>
        /// 
        public static void Save<T>(this T obj, Stream stream)
        {
            new BinaryFormatter().Serialize(stream, obj);
        }

        /// <summary>
        ///   Saves an object to a stream.
        /// </summary>
        /// 
        /// <param name="obj">The object to be serialized.</param>
        /// <param name="path">The path to the file to which the object is to be serialized.</param>
        /// 
        public static void Save<T>(this T obj, string path)
        {
            using (var fs = new FileStream(path, FileMode.Create))
            {
                Save(obj, fs);
            }
        }

        /// <summary>
        ///   Loads an object from a stream.
        /// </summary>
        /// 
        /// <param name="stream">The stream from which the object is to be deserialized.</param>
        /// 
        /// <returns>The deserialized machine.</returns>
        /// 
        public static T Load<T>(Stream stream)
        {
            return Load<T>(stream, new BinaryFormatter());
        }

        /// <summary>
        ///   Loads an object from a file.
        /// </summary>
        /// 
        /// <param name="path">The path to the file from which the object is to be deserialized.</param>
        /// 
        /// <returns>The deserialized object.</returns>
        /// 
        public static T Load<T>(string path)
        {
            using (var fs = new FileStream(path, FileMode.Open))
            {
                return Load<T>(fs);
            }
        }

        /// <summary>
        ///   Loads a model from a stream.
        /// </summary>
        /// 
        /// <typeparam name="T">The type of the model to be loaded.</typeparam>
        /// <param name="formatter">The binary formatter.</param>
        /// <param name="stream">The stream from which to deserialize the object graph.</param>
        /// 
        /// <returns></returns>
        /// 
        public static T Load<T>(Stream stream, BinaryFormatter formatter)
        {
            lock (lockObj)
            {
                try
                {
                    if (formatter.Binder == null)
                        formatter.Binder = GetBinder(typeof(T));

                    AppDomain.CurrentDomain.AssemblyResolve += resolve;
                    object obj = formatter.Deserialize(stream);

                    if (obj is T)
                        return (T)obj;
                    return obj.To<T>();
                }
                finally
                {
                    AppDomain.CurrentDomain.AssemblyResolve -= resolve;
                }
            }
        }



        private static SerializationBinder GetBinder(Type type)
        {
            var attribute = Attribute.GetCustomAttribute(type,
                typeof(SerializationBinderAttribute)) as SerializationBinderAttribute;

            if (attribute == null)
                return null;
            
            return attribute.Binder;
        }

        private static Assembly resolve(object sender, ResolveEventArgs args)
        {
            var display = new AssemblyName(args.Name);

            if (display.Name == args.Name)
                return null;

            return ((AppDomain)sender).Load(display.Name);
        }

    }
}
