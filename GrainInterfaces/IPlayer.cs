using Orleans;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GrainInterfaces
{
   
    public class StateSnapShot
    {
        public List<Guid> PlayerIds;
        public List<Guid> BallIds;
        public Guid LatestBallReceived;
    }
    public interface IPlayer : IGrainWithGuidKey, IRemindable
    {
        /// <summary>
        /// Initialize the player with knowledge of other players in the game and if he starts with 
        /// a ball
        /// </summary>
        /// <param name="playerIds">Other player (including himself, but not necessary)</param>
        /// <param name="isHoldingBall">True if he should start with a ball, false if not</param>
        /// <returns>Task to be awaited</returns>
        Task Initialize(IEnumerable<Guid> playerIds, bool isHoldingBall);
        /// <summary>
        /// Mark the primary player who should announce to the rest that they should take a snapshot
        /// </summary>
        /// <returns>Task to be awaited</returns>
        Task PrimaryMark();
        /// <summary>
        /// Returns a boolean showing if the player is marked, this is useful to check if the players 
        /// are ready to all send their snapshots or not
        /// </summary>
        /// <returns>Task to be awaited with result of true/false if the player is marked</returns>
        Task<bool> IsMarked();
        /// <summary>
        /// Mark the player and save a snapshot at the given time of the marking, this does not return
        /// a snapshot as it just says it should be saved in the playerstate
        /// </summary>
        /// <returns>Task to be awaited</returns>
        Task Mark();
        /// <summary>
        /// Receives the snapshots from the player, this should be called after all players return true
        /// on IsMarked() above
        /// </summary>
        /// <returns>Task to be awaited with result of a snapshot of relevant states</returns>
        Task<StateSnapShot> GetSnapShot();
        /// <summary>
        /// Tell the player to add the provided ball to their ball list
        /// </summary>
        /// <param name="ballId">Ball to be received by the player</param>
        /// <returns>Task to be awaited with result of the receiving players marking, relevant for snapshot</returns>
        Task<bool> ReceiveBall(Guid ballId);
        /// <summary>
        /// Gets the ball list of the player at the time of the call, this does not work to check all 
        /// players at once if we should concurrently get a count as transactions can happen between 
        /// fetching the list from each player
        /// </summary>
        /// <returns>Task to be awaited with result of a list over the balls the player contains</returns>
        Task<List<Guid>> GetBallIds();

    }
}
