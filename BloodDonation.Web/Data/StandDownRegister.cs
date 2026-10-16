using BloodDonation.Domain.Models;

namespace BloodDonation.Web.Data;

// Stand-down events each donor has entered, so a stand-down keeps blocking bookings
// after the donor stops entering it (#4). In memory, one instance per app.
// Locked because concurrent requests share it (unlike the lists in InMemoryStore, #17).
public class StandDownRegister
{
    private readonly object _lock = new();
    private readonly Dictionary<string, List<StandDownEvent>> _events = new();

    public void Record(string donorUserId, StandDownEvent standDownEvent)
    {
        lock (_lock)
        {
            if (!_events.TryGetValue(donorUserId, out var events))
            {
                events = new List<StandDownEvent>();
                _events[donorUserId] = events;
            }

            events.Add(standDownEvent);
        }
    }

    public IReadOnlyList<StandDownEvent> For(string donorUserId)
    {
        lock (_lock)
        {
            return _events.TryGetValue(donorUserId, out var events) ? events.ToList() : [];
        }
    }
}
