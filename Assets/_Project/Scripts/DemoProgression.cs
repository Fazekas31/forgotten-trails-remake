namespace ForgottenTrail.Gameplay.Progression
{
    public enum DemoObjective
    {
        FindLukeAtGate,
        FollowBootprintsToSaloon,
        InvestigateSaloonClues,
        ExamineSaloonKnife,
        DiscoverChurchTruth,
        SearchSheriffOffice,
        ReturnToChurch,
        ConfrontCreatureInBarn,
        ReachForest,
        Complete
    }

    /// <summary>Tracks the demo's narrative beats while leaving exploration within each area open.</summary>
    public sealed class DemoProgression
    {
        private static readonly DemoObjective[] Route =
        {
            DemoObjective.FindLukeAtGate,
            DemoObjective.FollowBootprintsToSaloon,
            DemoObjective.InvestigateSaloonClues,
            DemoObjective.ExamineSaloonKnife,
            DemoObjective.DiscoverChurchTruth,
            DemoObjective.SearchSheriffOffice,
            DemoObjective.ReturnToChurch,
            DemoObjective.ConfrontCreatureInBarn,
            DemoObjective.ReachForest,
            DemoObjective.Complete
        };

        private int _currentIndex;

        public DemoObjective CurrentObjective => Route[_currentIndex];
        public bool IsComplete => CurrentObjective == DemoObjective.Complete;

        public bool TryComplete(DemoObjective objective)
        {
            if (IsComplete || objective != CurrentObjective)
                return false;

            _currentIndex++;
            return true;
        }
    }
}
