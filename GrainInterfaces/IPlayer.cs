using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GrainInterfaces
{
   
    public interface IPlayer: Orleans.IGrainWithGuidKey //, Orleans.IRemindable
    {
        /// <summary>
        /// Give a list of other player names to the player and decide at 
        /// the beginning does this player hold a ball or not. 
        /// List<Guid> playerIds: the list of all players names
        /// bool isHoldingBall - false: at the beginning the palyer does not hold a ball
        ///                    - true: athe the beginning the palyer does hold a ball
        /// </summary>
        Task Initialize(IEnumerable<Guid> playerIds, bool isHoldingBall);
    
        /// <summary>
        /// The player receives a ball from another player, it gets the ball passed
        /// Guid ballId: the id of the ball be received.
        /// <summary>
        Task ReceiveBall(Guid ballId);

        /// <summary>
        /// /// The player receives a ball from the game maker
        /// Guid ballId: the id of the ball be received.
        /// <summary>
        Task GiveBallToPlayer(Guid ballId);

        /// <summary>
        /// /// The player receives a ball from the game maker
        /// Guid ballId: the id of the ball be received.
        /// <summary>
        Task HoldOrPassBallTruelyRandom(object ballId);

        /// <summary>
        /// Get the balls held by the player
        /// </summary>
        Task<List<Guid>> GetBallIds();

    }
}
