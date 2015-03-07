using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Skobbler.Ngx.Util;
using Java.IO;
using Java.Lang.Reflect;
using Java.Lang;
using Environment = Android.OS.Environment;
using Exception = System.Exception;
using System.IO;
using IOException = System.IO.IOException;

namespace Skobbler.Ngx.SDKTools.Download
{
    public class SKToolsDownloadUtils
    {

        /// <summary>
        /// the values used to convert the memory amount to the proper units(KB, MB, GB...)
        /// </summary>
        public const long KILO = 1024;

        public static readonly long MEGA = KILO * KILO;

        public static readonly long GIGA = MEGA * KILO;

        public static readonly long TERRA = GIGA * KILO;

        public static readonly long MINIMUM_FREE_MEMORY = 20 * MEGA;

        /// <summary>
        /// removes files/folders corresponding to a certain path </summary>
        /// <param name="currentLocationPath"> current location path </param>
        public static void removeCurrentLocationFromDisk(string currentLocationPath)
        {
            string deleteCmd = "rm -r " + currentLocationPath;
            Runtime runtime = Runtime.GetRuntime();
            try
            {
                runtime.Exec(deleteCmd);
                SKLogging.WriteLog(SKToolsDownloadPerformer.TAG, "The file was deleted from its current installation folder", SKLogging.LogDebug);
            }
            catch (IOException)
            {
                SKLogging.WriteLog(SKToolsDownloadPerformer.TAG, "The file couldn't be deleted !!!", SKLogging.LogDebug);
            }
        }

        /// <summary>
        /// gets the bytes needed to perform a download </summary>
        /// <param name="neededBytes"> number of bytes that should be available, on the device, for performing a download </param>
        /// <param name="path"> the path where resources will be downloaded </param>
        /// <returns> needed bytes in order to perform the current download, or 0 if there are enough available bytes is -1 if given path is wrong </returns>
        public static long getNeededBytesForADownload(long neededBytes, string path)
        {
            if (path == null)
            {
                return -1;
            }
            else
            {
                if (!isDataAccessible(path))
                {
                    return -1;
                }
                else if (path.StartsWith("/data", StringComparison.Ordinal))
                { // resources are on internal memory
                    long availableMemorySize = getAvailableMemorySize(Environment.DataDirectory.Path);
                    if ((neededBytes + MINIMUM_FREE_MEMORY) <= availableMemorySize)
                    {
                        return 0;
                    }
                    else
                    {
                        return (neededBytes + MINIMUM_FREE_MEMORY - availableMemorySize);
                    }
                }
                else
                { // resources are on other storage
                    string memoryPath = null;
                    int androidFolderIndex = path.IndexOf("/Android", StringComparison.Ordinal);
                    if ((androidFolderIndex > 0) && (androidFolderIndex < path.Length))
                    {
                        memoryPath = path.Substring(0, androidFolderIndex);
                    }
                    if (memoryPath == null)
                    {
                        return -1;
                    }
                    else
                    {
                        long availableMemorySize = getAvailableMemorySize(memoryPath);
                        if ((neededBytes + MINIMUM_FREE_MEMORY) <= availableMemorySize)
                        {
                            return 0;
                        }
                        else
                        {
                            return (neededBytes + MINIMUM_FREE_MEMORY - availableMemorySize);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// gets the available internal memory size </summary>
        /// <returns> available memory size in bytes </returns>
        public static long getAvailableMemorySize(string path)
        {
            StatFs statFs = null;
            try
            {
                statFs = new StatFs(path);
            }
            catch (System.ArgumentException ex)
            {
                SKLogging.WriteLog(SKToolsDownloadPerformer.TAG, "Exception when creating StatF ; message = " + ex, SKLogging.LogDebug);
            }
            if (statFs != null)
            {
                Method getAvailableBytesMethod = null;
                try
                {
                    getAvailableBytesMethod = statFs.Class.GetMethod("getAvailableBytes");
                }
                catch (NoSuchMethodException e)
                {
                    SKLogging.WriteLog(SKToolsDownloadPerformer.TAG, "Exception at getAvailableMemorySize method = " + e.Message, SKLogging.LogDebug);
                }

                if (getAvailableBytesMethod != null)
                {
                    try
                    {
                        SKLogging.WriteLog(SKToolsDownloadPerformer.TAG, "Using new API for getAvailableMemorySize method !!!", SKLogging.LogDebug);
                        return (long)getAvailableBytesMethod.Invoke(statFs);
                    }
                    catch (IllegalAccessException)
                    {
                        return (long)statFs.AvailableBlocks * (long)statFs.BlockSize;
                    }
                    catch (InvocationTargetException)
                    {
                        return (long)statFs.AvailableBlocks * (long)statFs.BlockSize;
                    }
                }
                else
                {
                    return (long)statFs.AvailableBlocks * (long)statFs.BlockSize;
                }
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// checks if data on this path is accessible </summary>
        /// <param name="path"> the path whose availability is checked </param>
        /// <returns> true if the data from the given path is accessible, false otherwise (data erased, SD card removed, etc) </returns>
        public static bool isDataAccessible(string path)
        {
            // if file is on internal memory, check its existence
            if (path != null)
            {
                if (path.StartsWith("/data", StringComparison.Ordinal))
                {
                    return System.IO.Directory.Exists(path) || System.IO.File.Exists(path);
                }
                else
                {
                    string memoryPath = null;
                    int androidFolderIndex = path.IndexOf("/Android", StringComparison.Ordinal);
                    if (androidFolderIndex > 0 && androidFolderIndex < path.Length)
                    {
                        memoryPath = path.Substring(0, androidFolderIndex);
                    }
                    if (memoryPath != null)
                    {
                        bool check = false;
                        try
                        {
                            FileStream fs = new FileStream("/proc/mounts", FileMode.OpenOrCreate); 
                            StreamReader @in = new StreamReader(fs);
                            //BufferedReader br = new BufferedReader(new InputStreamReader(@in));
                            BufferedReader br = null;
                            string strLine;
                            while ((strLine = br.ReadLine()) != null && !check)
                            {
                                if (strLine.Contains(memoryPath))
                                {
                                    check = true;
                                }
                            }
                            br.Close();
                        }
                        catch (Exception e)
                        {
                            SKLogging.WriteLog(SKToolsDownloadPerformer.TAG, "Exception in isDataAccessible method ; message = " + e.Message, SKLogging.LogDebug);
                        }
                        return check && System.IO.Directory.Exists(path) || System.IO.File.Exists(path);
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            else
            {
                return false;
            }
        }
    }
}