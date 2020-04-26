extends AudioStreamPlayer2D

export var stream_resource: AudioStreamSample

func _on_audio_stream_finished()->void:
  queue_free()

func _ready()->void:
  stream = stream_resource
  connect("finished", self, "_on_audio_stream_finished")

  call_deferred("play")
