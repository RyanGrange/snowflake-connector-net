using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using Mono.Unix;
using Mono.Unix.Native;
using NUnit.Framework;
using Snowflake.Data.Core;
using Snowflake.Data.Core.Tools;
using static Snowflake.Data.Tests.UnitTests.Configuration.EasyLoggingConfigGenerator;

namespace Snowflake.Data.Tests.Tools
{
    [TestFixture, NonParallelizable]
    public class UnixOperationsTest
    {
        private static UnixOperations s_unixOperations;
        private static readonly string s_workingDirectory = Path.Combine(Path.GetTempPath(), "easy_logging_test_configs_", Path.GetRandomFileName());

        [OneTimeSetUp]
        public static void BeforeAll()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return;
            if (!Directory.Exists(s_workingDirectory))
            {
                Directory.CreateDirectory(s_workingDirectory);
            }
            s_unixOperations = new UnixOperations();
        }

        [OneTimeTearDown]
        public static void AfterAll()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return;
            Directory.Delete(s_workingDirectory, true);
        }

        [Test]
        public void TestDetectGroupOrOthersWritablePermissions(
            [ValueSource(nameof(GroupOrOthersWritablePermissions))] FilePermissions groupOrOthersWritablePermissions,
            [ValueSource(nameof(GroupNotWritablePermissions))] FilePermissions groupNotWritablePermissions,
            [ValueSource(nameof(OtherNotWritablePermissions))] FilePermissions otherNotWritablePermissions)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.Ignore("skip test on Windows");
            }

            // arrange
            var filePath = CreateConfigTempFile(s_workingDirectory, "random text");
            var readWriteUserPermissions = FilePermissions.S_IRUSR | FilePermissions.S_IWUSR;
            var filePermissions = readWriteUserPermissions | groupOrOthersWritablePermissions | groupNotWritablePermissions | otherNotWritablePermissions;
            Syscall.chmod(filePath, filePermissions);

            // act
            var result = s_unixOperations.CheckFileHasAnyOfPermissions(filePath, FileAccessPermissions.GroupWrite | FileAccessPermissions.OtherWrite);

            // assert
            Assert.IsTrue(result);
        }

        [Test]
        public void TestDetectGroupOrOthersNotWritablePermissions(
            [ValueSource(nameof(UserPermissions))] FilePermissions userPermissions,
            [ValueSource(nameof(GroupNotWritablePermissions))] FilePermissions groupNotWritablePermissions,
            [ValueSource(nameof(OtherNotWritablePermissions))] FilePermissions otherNotWritablePermissions)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.Ignore("skip test on Windows");
            }

            var filePath = CreateConfigTempFile(s_workingDirectory, "random text");
            var filePermissions = userPermissions | groupNotWritablePermissions | otherNotWritablePermissions;
            Syscall.chmod(filePath, filePermissions);

            // act
            var result = s_unixOperations.CheckFileHasAnyOfPermissions(filePath, FileAccessPermissions.GroupWrite | FileAccessPermissions.OtherWrite);

            // assert
            Assert.IsFalse(result);
        }

        [Test]
        public void TestReadAllTextCheckingPermissionsUsingTomlConfigurationFileValidations(
            [ValueSource(nameof(UserAllowedPermissions))] FilePermissions userAllowedPermissions)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.Ignore("skip test on Windows");
            }
            var content = "random text";
            var filePath = CreateConfigTempFile(s_workingDirectory, content);
            Syscall.chmod(filePath, userAllowedPermissions);

            // act
            var result = s_unixOperations.ReadAllText(filePath, TomlConnectionBuilder.ValidateFilePermissions);

            // assert
            Assert.AreEqual(content, result);
        }

        [Test]
        public void TestFailIfGroupOrOthersHavePermissionsToFileWithTomlConfigurationValidations([ValueSource(nameof(UserReadWritePermissions))] FilePermissions userPermissions,
            [ValueSource(nameof(GroupPermissions))] FilePermissions groupPermissions,
            [ValueSource(nameof(OthersPermissions))] FilePermissions othersPermissions)
        {
            if(groupPermissions == 0 && othersPermissions == 0)
            {
                Assert.Ignore("Skip test when group and others have no permissions");
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.Ignore("skip test on Windows");
            }
            var content = "random text";
            var filePath = CreateConfigTempFile(s_workingDirectory, content);

            var filePermissions = userPermissions | groupPermissions | othersPermissions;
            Syscall.chmod(filePath, filePermissions);

            // act and assert
            Assert.Throws<SecurityException>(() => s_unixOperations.ReadAllText(filePath, TomlConnectionBuilder.ValidateFilePermissions), "Attempting to read a file with too broad permissions assigned");
        }

        public static IEnumerable<FilePermissions> UserPermissions()
        {
            yield return FilePermissions.S_IRUSR;
            yield return FilePermissions.S_IWUSR;
            yield return FilePermissions.S_IXUSR;
            yield return FilePermissions.S_IRUSR | FilePermissions.S_IWUSR | FilePermissions.S_IXUSR;
        }

        public static IEnumerable<FilePermissions> GroupPermissions()
        {
            yield return 0;
            yield return FilePermissions.S_IRGRP;
            yield return FilePermissions.S_IWGRP;
            yield return FilePermissions.S_IXGRP;
            yield return FilePermissions.S_IRGRP | FilePermissions.S_IWGRP | FilePermissions.S_IXGRP;
        }

        public static IEnumerable<FilePermissions> OthersPermissions()
        {
            yield return 0;
            yield return FilePermissions.S_IROTH;
            yield return FilePermissions.S_IWOTH;
            yield return FilePermissions.S_IXOTH;
            yield return FilePermissions.S_IROTH | FilePermissions.S_IWOTH | FilePermissions.S_IXOTH;
        }

        public static IEnumerable<FilePermissions> GroupOrOthersWritablePermissions()
        {
            yield return FilePermissions.S_IWGRP;
            yield return FilePermissions.S_IWOTH;
            yield return FilePermissions.S_IWGRP | FilePermissions.S_IWOTH;
        }

        public static IEnumerable<FilePermissions> GroupNotWritablePermissions()
        {
            yield return 0;
            yield return FilePermissions.S_IRGRP;
            yield return FilePermissions.S_IXGRP;
            yield return FilePermissions.S_IRGRP | FilePermissions.S_IXGRP;
        }

        public static IEnumerable<FilePermissions> OtherNotWritablePermissions()
        {
            yield return 0;
            yield return FilePermissions.S_IROTH;
            yield return FilePermissions.S_IXOTH;
            yield return FilePermissions.S_IROTH | FilePermissions.S_IXOTH;
        }

        public static IEnumerable<FilePermissions> UserReadWritePermissions()
        {
            yield return FilePermissions.S_IRUSR | FilePermissions.S_IWUSR;
        }

        public static IEnumerable<FilePermissions> UserAllowedPermissions()
        {
            yield return FilePermissions.S_IRUSR;
            yield return FilePermissions.S_IRUSR | FilePermissions.S_IWUSR;
        }

        public static IEnumerable<FilePermissions> GroupOrOthersReadablePermissions()
        {
            yield return 0;
            yield return FilePermissions.S_IRGRP;
            yield return FilePermissions.S_IROTH;
            yield return FilePermissions.S_IRGRP | FilePermissions.S_IROTH;
        }
    }
}
