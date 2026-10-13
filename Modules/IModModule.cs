using System;
using Il2CppScheduleOne.PlayerScripts;

namespace ArfSchedule1.Modules
{
    public interface IModModule
    {
        string Id { get; }
        string Name { get; }
        string Description { get; }
        string Category { get; }
        bool Enabled { get; set; }
        bool IsExpanded { get; set; }

        void OnInitialize();
        void OnUpdate(PlayerMovement pm);
        void OnDrawSettings();
    }
}
