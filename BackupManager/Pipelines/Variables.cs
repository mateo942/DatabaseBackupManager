using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace BackupManager.Pipelines
{
    public class Variables : Dictionary<string, object>
    {
        public const string BACKUP_PATH = "backup_path";
        public const string FILES_TO_DELETE = "files_to_delete";
        public const string FILES_TO_UPLOAD = "files_to_upload";
    }

    public static class VariablesExtensions
    {
        public static string SetBackupPath(this Variables source, string value)
        {
            source[Variables.BACKUP_PATH] = value;

            return value;
        }

        public static string GetBackupPath(this Variables source)
        {
            if(source.TryGetValue(Variables.BACKUP_PATH, out object t))
            {
                return t as string;
            }

            return null;
        }

        public static IEnumerable<string> GetFilesToDelete(this Variables source)
        {
            if (source.TryGetValue(Variables.FILES_TO_DELETE, out object t))
            {
                return t as IEnumerable<string>;
            }

            return null;
        }

        public static IEnumerable<string> AddFilesToDelete(this Variables source, string value)
        {
            List<string> list;
            if (source.TryGetValue(Variables.FILES_TO_DELETE, out object t))
            {
                list = t as List<string>;
            } else
            {
                list = new List<string>();
                source[Variables.FILES_TO_DELETE] = list;
            }

            list.Add(value);

            return list.AsEnumerable();
        }

        public static IEnumerable<string> GetFilesToUpload(this Variables source)
        {
            if (source.TryGetValue(Variables.FILES_TO_UPLOAD, out object t))
            {
                return t as IEnumerable<string>;
            }

            return null;
        }

        public static IEnumerable<string> AddFilesToUpload(this Variables source, string value)
        {
            List<string> list;
            if (source.TryGetValue(Variables.FILES_TO_UPLOAD, out object t))
            {
                list = t as List<string>;
            }
            else
            {
                list = new List<string>();
                source[Variables.FILES_TO_UPLOAD] = list;
            }

            list.Add(value);

            return list.AsEnumerable();
        }

        public static IEnumerable<string> RemoveFilesToUpload(this Variables source, Predicate<string> predicate)
        {
            List<string> list;
            if (source.TryGetValue(Variables.FILES_TO_UPLOAD, out object t))
            {
                list = t as List<string>;
            }
            else
            {
                list = new List<string>();
                source[Variables.FILES_TO_UPLOAD] = list;
            }

            list.RemoveAll(predicate);

            return list.AsEnumerable();
        }
    }
}
