namespace DotfilesLinker.Tests
{
    public class PathUtilitiesTests
    {
        [Fact]
        public void PathEquals_ignores_case_and_normalizes()
        {
            var a = @"C:\Foo\..\Bar\File.txt";
            var b = @"c:\bar\file.TXT";

            Assert.True(PathUtilities.PathEquals(a, b));
        }
    }
}
