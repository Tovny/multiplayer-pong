import { useEffect, useReducer, useRef, useState } from "react";
import "./App.css";
import { Ball } from "./components/game/Ball";
import { Paddle } from "./components/game/Paddle";
import { Score } from "./components/game/Score";
import { Header } from "./components/menu/Header";
import { Instructions } from "./components/menu/Instructions";
import { fromEvent, interval, tap } from "rxjs";
import { betweenPoints } from "./utils/between-points";
import {
  BALL_RADIUS,
  MAX_SCORE,
  PADDLE_HEIGHT,
  PADDLE_WIDTH,
} from "./constants/constants";
import { webSocket } from "rxjs/webSocket";

const initialState = {
  ballY: 50 - Math.random() * 20,
  ballX: 50,
  vx: 0.25 * (Math.random() < 0.5 ? 1 : -1),
  vy: 0.5 * (Math.random() < 0.5 ? 1 : -1),
  leftScore: 0,
  rightScore: 0,
  leftPaddleTop: 50 - PADDLE_HEIGHT / 2,
  rightPaddleTop: 50 - PADDLE_HEIGHT / 2,
};

function updateReducer(state, payload) {
  if (!payload) {
    return state;
  }
  return { ...state, ...payload };
}

const updatePaddle = (current, change) => {
  const newPos = current + change;
  const max = 100 - PADDLE_HEIGHT;
  if (newPos <= 0) {
    return 0;
  }
  if (newPos >= max) {
    return max;
  }
  return newPos;
};

function reducer(state, action) {
  switch (action?.type) {
    case "reset":
      return { ...initialState };
    case "leftPaddle":
      state.leftPaddleTop = updatePaddle(state.leftPaddleTop, action.payload);
      break;
    case "rightPaddle":
      state.rightPaddleTop = updatePaddle(state.rightPaddleTop, action.payload);
      break;
    default:
      state.ballX = state.ballX + state.vx;
      state.ballY = state.ballY + state.vy;

      const leftPaddleStart = PADDLE_WIDTH;
      if (state.ballX <= leftPaddleStart) {
        const betweenLeftPaddle = betweenPoints(
          state.ballY,
          state.leftPaddleTop,
          state.leftPaddleTop + PADDLE_HEIGHT
        );
        if (betweenLeftPaddle) {
          state.ballX = leftPaddleStart;
          state.vx = -state.vx;
          return { ...state };
        }
      }

      const rightPaddleStart = 100 - PADDLE_WIDTH;
      if (state.ballX >= rightPaddleStart) {
        const betweenRightPaddle = betweenPoints(
          state.ballX,
          state.rightPaddleTop,
          state.rightPaddleTop + PADDLE_HEIGHT
        );
        if (betweenRightPaddle) {
          state.ballX = rightPaddleStart;
          state.vx = -state.vx;
          return { ...state };
        }
      }

      if (state.ballX <= 0) {
        state.ballX = 0;
        state.vx = -state.vx;

        if (state.rightScore < MAX_SCORE) {
          state.rightScore = state.rightScore + 1;
        }
      }

      const rightBound = 100 - BALL_RADIUS;

      if (state.ballX >= rightBound) {
        state.ballX = rightBound;
        state.vx = -state.vx;

        if (state.leftScore < 11) {
          state.leftScore = state.leftScore + 1;
        }
      }

      if (state.ballY <= 0) {
        state.ballY = 0;
        state.vy = -state.vy;
      }

      const bottomBound = rightBound - BALL_RADIUS;

      if (state.ballY >= bottomBound) {
        state.ballY = bottomBound;
        state.vy = -state.vy;
      }
  }

  return { ...state };
}

export function App() {
  const [hasGameStarted, setHasGameStarted] = useState(false);
  const [showInstructions, setShowInstructions] = useState(true);
  const [level, setLevel] = useState(0);
  const [isGameOver, setIsGameOver] = useState(false);
  const [
    { leftPaddleTop: lpu, rightPaddleTop: rpt, ballX: bxu, ballY: byu },
    dispatch,
  ] = useReducer(reducer, initialState);
  const [
    { leftScore, rightScore, leftPaddleTop, rightPaddleTop, ballX, ballY },
    dispatch2,
  ] = useReducer(updateReducer, initialState);
  const leftPaddleRef = useRef(null);
  const rightPaddleRef = useRef(null);
  const ballRef = useRef(null);
  const [subject, setSubject] = useState();
  const [host, setHost] = useState(!window.chrome);

  useEffect(() => {
    const subject = webSocket("ws://localhost:5000");
    setSubject(subject);
    subject.subscribe((data) => {
      if (data.type === "start") {
        startButtonHandler();
      }
      if (data.payload) {
        dispatch2(data.payload);
      }
    });
  }, []);

  useEffect(() => {
    if (host) {
      subject?.next({
        action: "update",
        payload: {
          ballX: bxu,
          ballY: byu,
          leftPaddleTop: lpu,
        },
      });
    }
  }, [bxu, byu, lpu]);

  useEffect(() => {
    subject?.next({
      action: "update",
      payload: {
        rightPaddleTop: rpt,
      },
    });
  }, [rpt]);

  const onKeydown = (evt) => {
    const change = 2;
    const type = host ? "leftPaddle" : "rightPaddle";
    if (evt.key === "ArrowUp") {
      return dispatch({ type: type, payload: -change });
    }
    if (evt.key === "ArrowDown") {
      dispatch({ type: type, payload: change });
    }
  };

  const startButtonHandler = () => {
    dispatch({ type: "reset" });
    setHasGameStarted(true);
    startGame();
  };

  const startGame = () => {
    interval(1)
      .pipe(
        tap(() => {
          dispatch();
        })
      )
      .subscribe();
    fromEvent(document, "keydown")
      .pipe(
        tap((e) => {
          onKeydown(e);
        })
      )
      .subscribe();
  };

  const closeInstructions = () => {
    return setShowInstructions(false);
  };

  const renderGameOverText = () => {
    const winner = leftScore === 11 ? "Player 1" : "Player 2";
    return (
      leftScore === 11 ||
      (rightScore === 11 && (
        <div className="gameOver">
          <h1 style={{ display: "flex" }}>{`game over ${winner} wins`}</h1>
          <h3>Refresh page to play again</h3>
        </div>
      ))
    );
  };

  return (
    <div className="App">
      <div className="Game">
        <Header
          level={level}
          onClick={startButtonHandler}
          showButton={isGameOver || !hasGameStarted}
        />
        <Score position="left" player="1" total={leftScore} />
        {renderGameOverText()}
        <Instructions onClick={closeInstructions} visible={showInstructions} />
        <Score position="right" player="2" total={rightScore} />
        <Paddle
          name="p1"
          id="p1"
          tabIndex="0"
          ref={leftPaddleRef}
          y={leftPaddleTop}
          position="left"
        />
        <div className="vl" />
        <Ball x={ballX} y={ballY} ref={ballRef} />
        <Paddle y={rightPaddleTop} position="right" ref={rightPaddleRef} />
      </div>
    </div>
  );
}
