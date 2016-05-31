using System;
using System.Collections.Generic;
using System.IO;
using LibGit2Sharp;
using PowerArgs;
using UnreleasedGitHubHistory.Models;
using UnreleasedGitHubHistory.Providers;
using YamlDotNet.Serialization;

namespace UnreleasedGitHubHistory
{
    public class Config
    {
        private readonly ProgramArgs _programArgs;
        private const string YamlSettingsFileName = "UnreleasedGitHubHistory.yml";

        public Config(ProgramArgs programArgs)
        {
            _programArgs = programArgs;
        }

        public static bool GetCommandLineInput(string[] args, out ProgramArgs programArgs)
        {
            try
            {
                programArgs = Args.Parse<ProgramArgs>(args);
            }
            catch (ArgException e)
            {
                Console.WriteLine($"Error: {e.Message}");
                Console.WriteLine(ArgUsage.GenerateUsageFromTemplate<ProgramArgs>());
                programArgs = null;
                return false;
            }
            return new Config(programArgs).MergeAllUserInputs();
        }

        private bool MergeAllUserInputs()
        {
            if (!DiscoverGitRepository())
                return false;
            var yamlSettingsFile = Path.Combine(_programArgs.LocalGitRepository.Info.WorkingDirectory, YamlSettingsFileName);
            if (File.Exists(yamlSettingsFile))
                using (var reader = File.OpenText(yamlSettingsFile))
                    MergeWithYamlInput((new Deserializer()).Deserialize<ProgramArgs>(reader));
            MergeDefaults();
            GetEnvironmentVariableInput();
            if (!SetupPullRequestProvider())
                return false;
            return true;
        }

        private void GetEnvironmentVariableInput()
        {
            DiscoverGitHead();
            DiscoverGitVersion();
        }

        private bool DiscoverGitRepository()
        {
            if (string.IsNullOrWhiteSpace(_programArgs.GitRepositoryPath))
            {
                if (_programArgs.VerboseOutput)
                    Console.WriteLine($"GitRepositoryPath was not supplied. Trying to discover the Git repository from the current directory.");
                _programArgs.GitRepositoryPath = Directory.GetCurrentDirectory();
            }
            try
            {
                _programArgs.LocalGitRepository = new Repository(Repository.Discover(_programArgs.GitRepositoryPath));
            }
            catch (Exception ex) when (ex is ArgumentNullException || ex is ArgumentException || ex is RepositoryNotFoundException)
            {
                Console.WriteLine("GitRepositoryPath was not supplied or is invalid.");
                return false;
            }
            return true;
        }

        private void MergeWithYamlInput(ProgramArgs args)
        {
            _programArgs.ConfluenceApiUrl = _programArgs.ConfluenceApiUrl ?? args.ConfluenceApiUrl;
            _programArgs.ConfluenceReleaseParentPageId = _programArgs.ConfluenceReleaseParentPageId ?? args.ConfluenceReleaseParentPageId;
            _programArgs.ConfluenceSpaceKey = _programArgs.ConfluenceSpaceKey ?? args.ConfluenceSpaceKey;
            _programArgs.ExcludeLabel = _programArgs.ExcludeLabel ?? args.ExcludeLabel;
            _programArgs.FollowLabel = _programArgs.FollowLabel ?? args.FollowLabel;
            _programArgs.GitHubOwner = _programArgs.GitHubOwner ?? args.GitHubOwner;
            _programArgs.GitHubRepository = _programArgs.GitHubRepository ?? args.GitHubRepository;
            _programArgs.GitRemote = _programArgs.GitRemote ?? args.GitRemote;
            _programArgs.GitRepositoryPath = _programArgs.GitRepositoryPath ?? args.GitRepositoryPath;
            _programArgs.GitVersion = _programArgs.GitVersion ?? args.GitVersion;
            _programArgs.OutputFileName = _programArgs.OutputFileName ?? args.OutputFileName;
            _programArgs.ReleaseBranchRef = _programArgs.ReleaseBranchRef ?? args.ReleaseBranchRef;
            _programArgs.ReleaseNoteCategoryPrefix = _programArgs.ReleaseNoteCategoryPrefix ?? args.ReleaseNoteCategoryPrefix;
            _programArgs.ReleaseNoteDateFormat = _programArgs.ReleaseNoteDateFormat ?? args.ReleaseNoteDateFormat;
            _programArgs.ReleaseNoteFormat = _programArgs.ReleaseNoteFormat ?? args.ReleaseNoteFormat;
            _programArgs.ReleaseNoteOrderWhen = _programArgs.ReleaseNoteOrderWhen ?? args.ReleaseNoteOrderWhen;
            _programArgs.ReleaseNoteSectionlessDescription = _programArgs.ReleaseNoteSectionlessDescription ?? args.ReleaseNoteSectionlessDescription;
            _programArgs.ReleaseNoteUncategorisedDescription = _programArgs.ReleaseNoteUncategorisedDescription ?? args.ReleaseNoteUncategorisedDescription;
            _programArgs.GitLabOwner = _programArgs.GitLabOwner ?? args.GitLabOwner;
            _programArgs.GitLabRepository = _programArgs.GitLabRepository ?? args.GitLabRepository;
            _programArgs.GitLabApiUrl = _programArgs.GitLabApiUrl ?? args.GitLabApiUrl;
            _programArgs.GitLabProjectId = _programArgs.GitLabProjectId ?? args.GitLabProjectId;
            _programArgs.PullRequestProviderName = _programArgs.PullRequestProviderName ?? args.PullRequestProviderName;

            _programArgs.ReleaseNoteCategorised = _programArgs.ReleaseNoteCategorised ?? args.ReleaseNoteCategorised;
            _programArgs.ReleaseNoteOrderAscending = _programArgs.ReleaseNoteOrderAscending ?? args.ReleaseNoteOrderAscending;
            _programArgs.ReleaseNoteSectioned = _programArgs.ReleaseNoteSectioned ?? args.ReleaseNoteSectioned;
            _programArgs.ReleaseBranchHeadsOnly = _programArgs.ReleaseBranchHeadsOnly ?? args.ReleaseBranchHeadsOnly;

            _programArgs.AcceptInvalidCertificates = _programArgs.AcceptInvalidCertificates || args.AcceptInvalidCertificates;
            _programArgs.PublishToConfluence = _programArgs.PublishToConfluence || args.PublishToConfluence;
            _programArgs.PublishToFile = _programArgs.PublishToFile || args.PublishToFile;
            _programArgs.VerboseOutput = _programArgs.VerboseOutput || args.VerboseOutput;

            _programArgs.ReleaseNoteSections = _programArgs.ReleaseNoteSections ?? args.ReleaseNoteSections;
            _programArgs.ReleaseNoteCategories = _programArgs.ReleaseNoteCategories ?? args.ReleaseNoteCategories;
        }

        private void MergeDefaults()
        {
            _programArgs.ExcludeLabel = _programArgs.ExcludeLabel ?? "Exclude Note";
            _programArgs.FollowLabel = _programArgs.FollowLabel ?? "Follow Note";
            _programArgs.GitRemote = _programArgs.GitRemote ?? "origin";
            _programArgs.OutputFileName = _programArgs.OutputFileName ?? "Unreleased.md";
            _programArgs.ReleaseNoteCategoryPrefix = _programArgs.ReleaseNoteCategoryPrefix ?? "#";
            _programArgs.ReleaseNoteDateFormat = _programArgs.ReleaseNoteDateFormat ?? "MMM dd, yyyy HH:mm";
            _programArgs.ReleaseNoteFormat = _programArgs.ReleaseNoteFormat ?? "{0} {1}";
            _programArgs.ReleaseNoteOrderWhen = _programArgs.ReleaseNoteOrderWhen ?? "merged";
            _programArgs.ReleaseNoteSectionlessDescription = _programArgs.ReleaseNoteSectionlessDescription ?? "Undefined";
            _programArgs.ReleaseNoteUncategorisedDescription = _programArgs.ReleaseNoteUncategorisedDescription ?? "Unclassified";
            _programArgs.GitLabApiUrl = _programArgs.GitLabApiUrl ?? "https://gitlab.com/api/v3";

            _programArgs.ReleaseNoteCategorised = _programArgs.ReleaseNoteCategorised ?? false;
            _programArgs.ReleaseNoteOrderAscending = _programArgs.ReleaseNoteOrderAscending ?? false;
            _programArgs.ReleaseNoteSectioned = _programArgs.ReleaseNoteSectioned ?? false;
            _programArgs.ReleaseBranchHeadsOnly = _programArgs.ReleaseBranchHeadsOnly ?? true;

            _programArgs.ReleaseNoteSections = _programArgs.ReleaseNoteSections ?? new List<string>() { "bug=Fixes", "enhancement=Enhancements" };
        }

        private bool SetupPullRequestProvider()
        {
            _programArgs.PullRequestProviderName = _programArgs.PullRequestProviderName ?? "github";
            switch (_programArgs.PullRequestProviderName.ToLower())
            {
                case "github":
                    _programArgs.PullRequestProvider = new GitHubPullRequestProvider(_programArgs);
                    break;
                case "gitlab":
                    _programArgs.PullRequestProvider = new GitLabPullRequestProvider(_programArgs);
                    break;
                default:
                    Console.WriteLine($"Unsupported pull request provider: {_programArgs.PullRequestProvider}.");
                    return false;
            }
            if (!_programArgs.PullRequestProvider.DiscoverRemote())
                return false;
            return true;
        }

        private void DiscoverGitHead()
        {
            if (!string.IsNullOrWhiteSpace(_programArgs.ReleaseBranchRef))
            {
                if (!_programArgs.ReleaseBranchRef.CaseInsensitiveContains("refs/heads") && !_programArgs.ReleaseBranchRef.CaseInsensitiveContains("/"))
                    _programArgs.ReleaseBranchRef = $"refs/heads/{_programArgs.ReleaseBranchRef}";
                return;
            }
                
            if (_programArgs.VerboseOutput)
                Console.WriteLine($"ReleaseBranchRef was not supplied. Using the current HEAD branch.");
            _programArgs.ReleaseBranchRef = _programArgs.LocalGitRepository.Head.CanonicalName;
        }

        private void DiscoverGitVersion()
        {
            if (string.IsNullOrWhiteSpace(_programArgs.GitVersion))
                _programArgs.GitVersion = Environment.GetEnvironmentVariable("GITVERSION_MAJORMINORPATCH");
        }
     
        public void WriteSampleConfig()
        {
            var sampleConfigFile = Path.Combine(_programArgs.LocalGitRepository.Info.WorkingDirectory, YamlSettingsFileName);
            if (File.Exists(sampleConfigFile))
            {
                Console.WriteLine($"UnreleasedGitHubHistory.yml file already exists ...");
                return;
            }
            using (var writer = new StreamWriter(sampleConfigFile))
            {
                writer.WriteLine("# release-branch-heads-only: true");
                writer.WriteLine("# release-note-exclude: Exclude Note");
                writer.WriteLine("# release-note-follow: Follow Note");
                writer.WriteLine("# release-note-format: \"{0} {1}\"");
                writer.WriteLine("# release-note-date-format: \"MMM dd, yyyy HH:mm\"");
                writer.WriteLine("# release-note-sectioned: true");
                writer.WriteLine("# release-note-order-when: merged");
                writer.WriteLine("# release-note-sectionless-description: Undefined");
                writer.WriteLine("# release-note-sections:");
                writer.WriteLine("#   - bug=Fixes");
                writer.WriteLine("#   - enhancement=Enhancements");
            }
            Console.WriteLine($"Created a sample UnreleasedGitHubHistory.yml file ...");
        }
    }
}