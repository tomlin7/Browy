using System;
using System.IO;
using System.Xml.Linq;
using Xunit;

namespace Browy.Tests
{
    public class RepositoryIntegrityTests
    {
        [Fact]
        public void VersionFile_Exists_AndMatchesDirectoryBuildProps()
        {
            // Find repo root (traverse up from test output directory)
            string currentDir = AppDomain.CurrentDomain.BaseDirectory;
            string? repoRoot = null;
            var dir = new DirectoryInfo(currentDir);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "Browy.sln")))
                {
                    repoRoot = dir.FullName;
                    break;
                }
                dir = dir.Parent;
            }

            Assert.NotNull(repoRoot);

            string versionFilePath = Path.Combine(repoRoot, "VERSION");
            Assert.True(File.Exists(versionFilePath), "VERSION file must exist at repo root.");

            string versionContent = File.ReadAllText(versionFilePath).Trim();
            Assert.NotEmpty(versionContent);

            string propsFilePath = Path.Combine(repoRoot, "Directory.Build.props");
            Assert.True(File.Exists(propsFilePath), "Directory.Build.props must exist at repo root.");

            var xdoc = XDocument.Load(propsFilePath);
            var versionPrefixEl = xdoc.Root?.Element("PropertyGroup")?.Element("VersionPrefix");
            Assert.NotNull(versionPrefixEl);
            Assert.Equal(versionContent, versionPrefixEl.Value.Trim());
        }

        [Fact]
        public void LandingPage_Exists_AndContainsRequiredProductSections()
        {
            string currentDir = AppDomain.CurrentDomain.BaseDirectory;
            string? repoRoot = null;
            var dir = new DirectoryInfo(currentDir);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "Browy.sln")))
                {
                    repoRoot = dir.FullName;
                    break;
                }
                dir = dir.Parent;
            }

            Assert.NotNull(repoRoot);

            string wwwDir = Path.Combine(repoRoot, "www");
            Assert.True(Directory.Exists(wwwDir), "www/ directory must exist at repo root.");

            string packageJsonPath = Path.Combine(wwwDir, "package.json");
            string indexPath = Path.Combine(wwwDir, "index.html");
            string appTsxPath = Path.Combine(wwwDir, "src", "App.tsx");

            Assert.True(File.Exists(packageJsonPath), "www/package.json must exist.");
            Assert.True(File.Exists(indexPath), "www/index.html must exist.");
            Assert.True(File.Exists(appTsxPath), "www/src/App.tsx must exist.");

            string packageJson = File.ReadAllText(packageJsonPath);
            Assert.Contains("tailwindcss", packageJson);
            Assert.Contains("lucide-react", packageJson);

            string appTsx = File.ReadAllText(appTsxPath);
            Assert.Contains("Browy", appTsx);
            Assert.Contains("AI-Native", appTsx);
            Assert.Contains("Autonomous AI Copilot", appTsx);
            Assert.Contains("Spaces", appTsx);
            Assert.Contains("Download", appTsx);
            Assert.Contains("Benchmarks", appTsx);
            Assert.Contains("FAQ", appTsx);
            Assert.Contains("github.com/tomlin7/Browy", appTsx);
        }

        [Fact]
        public void WorkflowCiCd_Exists_AndContainsVerificationSteps()
        {
            string currentDir = AppDomain.CurrentDomain.BaseDirectory;
            string? repoRoot = null;
            var dir = new DirectoryInfo(currentDir);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "Browy.sln")))
                {
                    repoRoot = dir.FullName;
                    break;
                }
                dir = dir.Parent;
            }

            Assert.NotNull(repoRoot);

            string workflowPath = Path.Combine(repoRoot, ".github", "workflows", "ci-cd.yml");
            Assert.True(File.Exists(workflowPath), ".github/workflows/ci-cd.yml must exist.");

            string yml = File.ReadAllText(workflowPath);
            Assert.Contains("ci-checks", yml);
            Assert.Contains("dotnet test Browy.sln", yml);
            Assert.Contains("build-and-release", yml);
            Assert.Contains("deploy-landing-page", yml);
        }
    }
}
