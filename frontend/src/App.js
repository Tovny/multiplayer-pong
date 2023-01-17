import { useReducer, useRef, useState } from "react";
import "./App.css";
import { Ball } from "./components/game/Ball";
import { Paddle } from "./components/game/Paddle";
import { Score } from "./components/game/Score";
import { Header } from "./components/menu/Header";
import { Instructions } from "./components/menu/Instructions";
import { fromEvent, interval, tap } from "rxjs";
import * as R from "ramda";
import { betweenPoints } from "./utils/between-points";
import {
  BALL_RADIUS,
  MAX_SCORE,
  PADDLE_HEIGHT,
  PADDLE_WIDTH,
} from "./constants/constants";

const gameStartHeight = R.subtract(
  R.divide(window.innerHeight, 2),
  PADDLE_HEIGHT
);

const initialState = {
  ballY: Math.floor(window.innerHeight / 2),
  ballX: Math.floor(window.innerWidth / 2),
  vx: 2 * (Math.random() < 0.5 ? 1 : -1),
  vy: 2 * (Math.random() < 0.5 ? 1 : -1),
  boardBoundsRight: window.innerWidth + 10,
  leftScore: 0,
  rightScore: 0,
  leftPaddleTop: gameStartHeight,
  rightPaddleTop: 15,
};

function reducer(state, action) {
  switch (action?.type) {
    case "reset":
      return { ...initialState };
    case "leftPaddle":
      state.leftPaddleTop = state.leftPaddleTop + action.payload;
      break;
    case "rightPaddle":
      state.rightPaddleTop = state.rightPaddleTop + action.payload;
      break;
    default:
      state.ballX = state.ballX + state.vx;
      if (
        state.ballX <= PADDLE_WIDTH &&
        betweenPoints(
          state.ballY,
          state.leftPaddleTop,
          state.leftPaddleTop + PADDLE_HEIGHT
        )
      ) {
        state.ballX = PADDLE_WIDTH;
        state.vx = R.negate(state.vx);
        return { ...state };
      }
      if (
        state.ballX >= window.innerWidth - PADDLE_WIDTH &&
        betweenPoints(
          state.ballX,
          state.rightPaddleTop,
          state.rightPaddleTop + PADDLE_HEIGHT
        )
      ) {
        state.ballX = window.innerWidth - PADDLE_WIDTH;
        state.vx = R.negate(state.vx);
        return { ...state };
      }

      if (state.ballX <= 0) {
        state.ballX = 0;
        state.vx = R.negate(state.vx);
        if (
          state.rightScore < MAX_SCORE &&
          !betweenPoints(
            state.ballY,
            state.leftPaddleTop,
            state.leftPaddleTop + PADDLE_HEIGHT
          )
        ) {
          state.rightScore = state.rightScore + 1;
        }
      }

      if (state.ballX >= window.innerWidth - BALL_RADIUS) {
        state.ballX = window.innerWidth - BALL_RADIUS;
        state.vx = R.negate(state.vx);
        if (
          state.leftScore < 11 &&
          !betweenPoints(
            state.ballY,
            state.rightPaddleTop,
            state.rightPaddleTop + PADDLE_HEIGHT
          )
        ) {
          state.leftScore = state.leftScore + 1;
        }
      }
      state.ballY = state.ballY + state.vy;
      if (state.ballY <= 0) {
        state.ballY = 0;
        state.vy = R.negate(state.vy);
      }
      if (state.ballY >= window.innerHeight - BALL_RADIUS) {
        state.ballY = window.innerHeight - BALL_RADIUS;
        state.vy = R.negate(state.vy);
      }
  }

  return { ...state };
}

export function App() {
  const boardBoundsRight = window.innerWidth + 10;
  const [hasGameStarted, setHasGameStarted] = useState(false);
  const [showInstructions, setShowInstructions] = useState(true);
  const [level, setLevel] = useState(0);
  const [isGameOver, setIsGameOver] = useState(false);
  const [
    { leftScore, rightScore, leftPaddleTop, rightPaddleTop, ballX, ballY },
    dispatch,
  ] = useReducer(reducer, initialState);
  const leftPaddleRef = useRef(null);
  const rightPaddleRef = useRef(null);
  const ballRef = useRef(null);

  const onKeyDown = (evt) => {
    const change = 15;
    if (evt.key === "ArrowUp" && leftPaddleRef.current.offsetTop > 0) {
      return dispatch({ type: "leftPaddle", payload: -change });
    }
    if (
      evt.key === "ArrowDown" &&
      leftPaddleRef.current.offsetTop + PADDLE_HEIGHT < window.innerHeight
    ) {
      dispatch({ type: "leftPaddle", payload: change });
    }
  };

  const startButtonHandler = (e) => {
    dispatch({ type: "reset" });
    setHasGameStarted(true);
    e.target.blur();
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
          onKeyDown(e);
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
          x={5}
          tabIndex="0"
          ref={leftPaddleRef}
          y={leftPaddleTop}
          position="left"
        />
        <div className="vl" />
        <Ball x={ballX} y={ballY} ref={ballRef} />
        <Paddle
          x={R.subtract(boardBoundsRight, 30)}
          y={rightPaddleTop}
          position="right"
          ref={rightPaddleRef}
        />
      </div>
    </div>
  );
}