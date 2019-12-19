# RaEBS_2

This project was made in correlation to the new subject Reactive and Event-Based systems. The implementation provided here consist of two parts.

<h3>Task</h3>
We were tasked to create a program using Orleans to simulate a ball-game, where it would consist of N players and K balls (K < N). In exercise one we just needed to get the players playing and in exercise two, we have to implement a snapshot feature to take a snapshot of each players' state without stopping any on-going transactions, while still preserving the state before any future transactions happened after the snapshot was called.

<h2>Implementation part 1: Debugging and run-time testing</h2>
The original implementation provided was given in an x-unit testcase format, which in our experience is not good for testing initial iterations of a program, but much rather for the correctness. Therefore after many hours of trial and error, we get a silo and client solution working, which we use to get run-time output from the grains and their activities. It has to be said that a lot of the time, when running multiple threads and writing to output will result in some writes being lost, which is why it may sometimes seem to be missing an action, despite actually succeeding (later confirmed with exercise 2).

In the implementation we have provided an option to run the <b>Silo</b> project with static players between 1-10 and balls 1-9, so we could get feedback as such:

<code>
Player 1: Reminder received I have: [1, 2, 3, 4, 5, 6, 7, 8, 9,]
Player 1: PassOtherBalls, started and my latest received 9
Player 1: Torsing ball 1
Player 1: PassOtherBalls, tossing ball 1 to player 3
Player 3: Received ball 1
Player 1: Torsing ball 2
Player 1: PassOtherBalls, tossing ball 2 to player 3
Player 3: Received ball 2
Player 1: Torsing ball 3
Player 1: PassOtherBalls, tossing ball 3 to player 7
Player 7: Received ball 3
Player 1: Torsing ball 4
Player 1: PassOtherBalls, tossing ball 4 to player 2
Player 2: Received ball 4
Player 1: Torsing ball 5
</code>

The above was a sample from our Silo test with 10 static players (to get integer numeration) with 9 balls where all 9 started on the first player where he then would proceed to toss all of them *but* one to other player, which would then be followed by the decision of keeping or tossing the last ball. 

<h2>Implementation part 2: Correctness tests using XUnit</h2>
After we got a functioning program for exercise one, we then needed to test if the invarient kept. This is where XUnit is great to test the invariant of outcomes, such as how many balls were in player after x-time units. But we first needed to implement the snapshot feature, which is described more in detail in the report.
