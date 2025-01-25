import cv2
import grpc
import detection_pb2
import detection_pb2_grpc
import time

def stream_video(source_id: str, server_address: str):
    channel = grpc.insecure_channel(server_address)
    stub = detection_pb2_grpc.DetectionServiceStub(channel)

    cap = cv2.VideoCapture(0)  
    if not cap.isOpened():
        print("Error: Could not open video source.")
        return

    def generate_frames():
        while cap.isOpened():
            ret, frame = cap.read()
            if not ret:
                print("Stream ended or error reading frame.")
                break

            _, buffer = cv2.imencode(".jpg", frame)

            yield detection_pb2.DetectionRequest(
                source=source_id,
                image=buffer.tobytes()
            )

            time.sleep(0.03)

    try:
        response = stub.StreamDetections(generate_frames())
        print("Server Response:", response.status)
    except grpc.RpcError as e:
        print(f"gRPC error: {e.details()}")
    finally:
        cap.release()

if __name__ == "__main__":
    source_id = "camera1" 
    server_address = "localhost:50051" 
    stream_video(source_id, server_address)
