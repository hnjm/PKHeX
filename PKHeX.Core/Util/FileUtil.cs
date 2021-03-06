﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace PKHeX.Core
{
    /// <summary>
    /// Logic for detecting supported binary object formats.
    /// </summary>
    public static class FileUtil
    {
        /// <summary>
        /// Attempts to get a binary object from the provided path.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="reference">Reference savefile used for PC Binary compatibility checks.</param>
        /// <returns>Supported file object reference, null if none found.</returns>
        public static object GetSupportedFile(string path, SaveFile reference = null)
        {
            try
            {
                var fi = new FileInfo(path);
                if (IsFileTooBig(fi.Length) || IsFileTooSmall(fi.Length))
                    return null;

                var data = File.ReadAllBytes(path);
                var ext = Path.GetExtension(path);
                return GetSupportedFile(data, ext, reference);
            }
            catch (Exception e)
            {
                Debug.WriteLine(MessageStrings.MsgFileInUse);
                Debug.WriteLine(e.Message);
                return null;
            }
        }

        /// <summary>
        /// Attempts to get a binary object from the provided inputs.
        /// </summary>
        /// <param name="data">Binary data for the file.</param>
        /// <param name="ext">File extension used as a hint.</param>
        /// <param name="reference">Reference savefile used for PC Binary compatibility checks.</param>
        /// <returns>Supported file object reference, null if none found.</returns>
        public static object GetSupportedFile(byte[] data, string ext, SaveFile reference = null)
        {
            if (TryGetSAV(data, out var sav))
                return sav;
            if (TryGetMemoryCard(data, out var mc))
                return mc;
            if (TryGetPKM(data, out var pk, ext))
                return pk;
            if (TryGetPCBoxBin(data, out IEnumerable<byte[]> pks, reference))
                return pks;
            if (TryGetBattleVideo(data, out BattleVideo bv))
                return bv;
            if (TryGetMysteryGift(data, out MysteryGift g, ext))
                return g;
            return null;
        }

        /// <summary>
        /// Checks if the length is too big to be a detectable file.
        /// </summary>
        /// <param name="length">File size</param>
        public static bool IsFileTooBig(long length)
        {
            if (length <= 0x100000)
                return false;
            if (length == SaveUtil.SIZE_G4BR)
                return false;
            if (SAV3GCMemoryCard.IsMemoryCardSize(length))
                return false; // pbr/GC have size > 1MB
            return true;
        }

        /// <summary>
        /// Checks if the length is too small to be a detectable file.
        /// </summary>
        /// <param name="length">File size</param>
        public static bool IsFileTooSmall(long length) => length < 0x20;

        /// <summary>
        /// Tries to get an <see cref="SaveFile"/> object from the input parameters.
        /// </summary>
        /// <param name="data">Binary data</param>
        /// <param name="sav">Output result</param>
        /// <returns>True if file object reference is valid, false if none found.</returns>
        public static bool TryGetSAV(byte[] data, out SaveFile sav)
        {
            sav = SaveUtil.GetVariantSAV(data);
            return sav != null;
        }

        /// <summary>
        /// Tries to get an <see cref="SAV3GCMemoryCard"/> object from the input parameters.
        /// </summary>
        /// <param name="data">Binary data</param>
        /// <param name="memcard">Output result</param>
        /// <returns>True if file object reference is valid, false if none found.</returns>
        public static bool TryGetMemoryCard(byte[] data, out SAV3GCMemoryCard memcard)
        {
            if (!SAV3GCMemoryCard.IsMemoryCardSize(data))
            {
                memcard = null;
                return false;
            }
            memcard = new SAV3GCMemoryCard(data);
            return true;
        }

        /// <summary>
        /// Tries to get an <see cref="PKM"/> object from the input parameters.
        /// </summary>
        /// <param name="data">Binary data</param>
        /// <param name="pk">Output result</param>
        /// <param name="ext">Format hint</param>
        /// <param name="sav">Reference savefile used for PC Binary compatibility checks.</param>
        /// <returns>True if file object reference is valid, false if none found.</returns>
        public static bool TryGetPKM(byte[] data, out PKM pk, string ext, ITrainerInfo sav = null)
        {
            if (ext == ".pgt") // size collision with pk6
            {
                pk = default(PKM);
                return false;
            }
            var format = PKX.GetPKMFormatFromExtension(ext, sav?.Generation ?? 6);
            pk = PKMConverter.GetPKMfromBytes(data, prefer: format);
            return pk != null;
        }

        /// <summary>
        /// Tries to get an <see cref="IEnumerable{T}"/> object from the input parameters.
        /// </summary>
        /// <param name="data">Binary data</param>
        /// <param name="pkms">Output result</param>
        /// <param name="SAV">Reference savefile used for PC Binary compatibility checks.</param>
        /// <returns>True if file object reference is valid, false if none found.</returns>
        public static bool TryGetPCBoxBin(byte[] data, out IEnumerable<byte[]> pkms, SaveFile SAV)
        {
            if (SAV == null)
            {
                pkms = Enumerable.Empty<byte[]>();
                return false;
            }
            var length = data.Length;
            if (PKX.IsPKM(length / SAV.SlotCount) || PKX.IsPKM(length / SAV.BoxSlotCount))
            {
                pkms = PKX.GetPKMDataFromConcatenatedBinary(data, length);
                return true;
            }
            pkms = Enumerable.Empty<byte[]>();
            return false;
        }

        /// <summary>
        /// Tries to get a <see cref="BattleVideo"/> object from the input parameters.
        /// </summary>
        /// <param name="data">Binary data</param>
        /// <param name="bv">Output result</param>
        /// <returns>True if file object reference is valid, false if none found.</returns>
        public static bool TryGetBattleVideo(byte[] data, out BattleVideo bv)
        {
            bv = BattleVideo.GetVariantBattleVideo(data);
            return bv != null;
        }

        /// <summary>
        /// Tries to get a <see cref="MysteryGift"/> object from the input parameters.
        /// </summary>
        /// <param name="data">Binary data</param>
        /// <param name="mg">Output result</param>
        /// <param name="ext">Format hint</param>
        /// <returns>True if file object reference is valid, false if none found.</returns>
        public static bool TryGetMysteryGift(byte[] data, out MysteryGift mg, string ext)
        {
            mg = MysteryGift.GetMysteryGift(data, ext);
            return mg != null;
        }
    }
}
