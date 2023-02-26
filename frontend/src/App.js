import { useEffect, useReducer, useRef, useState } from "react";
import "./App.css";
import { Ball } from "./components/game/Ball";
import { Paddle } from "./components/game/Paddle";
import { Score } from "./components/game/Score";
import { fromEvent, tap } from "rxjs";
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
      leftPaddleY: leftPaddleTop,
      rightPaddleY: rightPaddleTop,
      ballX,
      ballY,
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

  useEffect(() => {
    const subject = webSocket("ws://localhost:5125");
    setSubject(subject);
    subject.subscribe((data) => {
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
      if (data.ballX) {
        dispatch2(data);
        if (data.winner) {
          setHasGameStarted(false);
        }
      }
    });
  }, []);

  const [paddleUpdate, setPaddleUpdate] = useState();

  const onKeydown = (evt) => {
    const change = 2;
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
      subject?.next({ paddle: paddleUpdate, action: type });
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
        })
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
            player={`1${host ? " (You)" : ""}`}
            total={leftScore}
          />
          <Score
            position="right"
            player={`2${!host ? " (You)" : ""}`}
            total={rightScore}
          />
          <Paddle
            name="p1"
            id="p1"
            tabIndex="0"
            ref={leftPaddleRef}
            y={leftPaddleTop}
            position="left"
            isPlayer={host}
          />
          <div className="vl" />
          <Ball x={ballX} y={ballY} ref={ballRef} />
          <Paddle
            y={rightPaddleTop}
            position="right"
            ref={rightPaddleRef}
            isPlayer={!host}
          />
        </div>
      )}
    </div>
  );
}
