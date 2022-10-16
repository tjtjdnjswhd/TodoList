namespace TodoList.Migrations
{
    public static class MigrationUtility
    {
        public static string ReadSql(Type migrationType, string sqlFileName)
        {
            var assembly = migrationType.Assembly;
            string resourceName = $"{migrationType.Namespace}.{sqlFileName}";
            using Stream? stream = assembly.GetManifestResourceStream(resourceName);

            if (stream == null)
            {
                throw new FileNotFoundException("SQL 파일을 찾을 수 없습니다", resourceName);
            }

            using var reader = new StreamReader(stream);
            string content = reader.ReadToEnd();
            return content;
        }
    }
}