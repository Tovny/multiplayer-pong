import { useEffect, useReducer, useRef, useState } from "react";
import "./App.css";
import { Ball } from "./components/game/Ball";
import { Paddle } from "./components/game/Paddle";
import { Score } from "./components/game/Score";
import { catchError, fromEvent, Subject, takeUntil, tap } from "rxjs";
import { webSocket } from "rxjs/webSocket";
import { Players } from "./components/menu/Players";

function updateReducer(state, payload) {
  if (!payload) {
    return state;
  }
  return { ...state, ...payload };
}

export function App() {
  const [hasGameStarted, setHasGameStarted] = useState(false);
  const [
    {
      leftScore,
      rightScore,
      leftPaddleY,
      rightPaddleY,
      ballX,
      ballY,
      player1,
      player2,
    },
    dispatch2,
  ] = useReducer(updateReducer, {});
  const leftPaddleRef = useRef(null);
  const rightPaddleRef = useRef(null);
  const ballRef = useRef(null);
  const [subject, setSubject] = useState();
  const [host, setHost] = useState(true);
  const [players, setPlayers] = useState([]);
  const [requestId, setRequestId] = useState();
  const [paddleUpdate, setPaddleUpdate] = useState();
  const [username, setUsername] = useState();
  const destroy$ = new Subject();

  useEffect(() => {
    handleInit("Please select a username");
  }, []);

  const handleInit = (msg) => {
    const username = prompt(msg);
    if (!username) {
      return handleInit("Username can't be empty");
    }
    setUsername(username);
    const subject = webSocket("ws://localhost:5125/" + username);
    setSubject(subject);
    subject.subscribe({
      next: (data) => {
        if (data.action === "playerUpdate") {
          setPlayers(data.payload);
        }
        if (data.action === "gameRequest") {
          setRequestId(data.payload);
        }
        if (data.action === "startGame") {
          setHasGameStarted(true);
          startButtonHandler();
        }
        if (data.action === "gameUpdate") {
          dispatch2(data.payload);
          if (data.payload.winner || data.payload.cancelled) {
            destroy$.next();
            setHasGameStarted(false);
          }
        }
      },
      error: () => handleInit("Username taken, please select another"),
    });
  };

  const onKeydown = (evt) => {
    const change = 3;
    if (evt.key === "ArrowUp") {
      setPaddleUpdate(-change);
    }
    if (evt.key === "ArrowDown") {
      setPaddleUpdate(change);
    }
  };

  useEffect(() => {
    if (paddleUpdate) {
      const type = host ? "leftPaddleChange" : "rightPaddleChange";
      subject?.next({ action: type, payload: paddleUpdate.toString() });
      setPaddleUpdate(undefined);
    }
  }, [paddleUpdate]);

  const startButtonHandler = () => {
    setHasGameStarted(true);
    startGame();
  };

  const startGame = () => {
    fromEvent(document, "keydown")
      .pipe(
        tap((e) => {
          onKeydown(e);
        }),
        takeUntil(destroy$)
      )
      .subscribe();
  };

  const sendGameRequest = (id) => {
    subject?.next({
      action: "gameRequest",
      payload: id,
    });
  };

  const acceptGameRequest = () => {
    subject?.next({
      action: "acceptGameRequest",
      payload: requestId,
    });
    setHost(false);
    setRequestId(undefined);
  };

  return (
    <div className="App">
      {requestId && (
        <button onClick={acceptGameRequest}>Accept game request</button>
      )}
      {!hasGameStarted && (
        <Players players={players} handleClick={sendGameRequest} />
      )}
      {hasGameStarted && (
        <div className="Game">
          <Score
            position="left"
            players={[player1, player2]}
            total={leftScore}
            isPlayer={host}
            username={username}
          />
          <Score
            position="right"
            players={[player1, player2]}
            total={rightScore}
            isPlayer={!host}
            username={username}
          />
          <Paddle
            name="p1"
            id="p1"
            tabIndex="0"
            ref={leftPaddleRef}
            y={leftPaddleY}
            position="left"
            isPlayer={host}
          />
          <div className="vl" />
          <Ball x={ballX} y={ballY} ref={ballRef} />
          <Paddle
            y={rightPaddleY}
            position="right"
            ref={rightPaddleRef}
            isPlayer={!host}
          />
        </div>
      )}
    </div>
  );
}
