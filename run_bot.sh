tmux kill-server
cd MerlinBot_Service
dotnet build
tmux new-session -d -s MerlinBot_session 
tmux send-keys -t MerlinBot_session "dotnet run" Enter