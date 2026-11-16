package com.readlens.ocr;

import android.graphics.Bitmap;
import android.graphics.Matrix;
import android.graphics.Rect;

import com.google.mlkit.vision.common.InputImage;
import com.google.mlkit.vision.text.Text;
import com.google.mlkit.vision.text.TextRecognition;
import com.google.mlkit.vision.text.TextRecognizer;
import com.google.mlkit.vision.text.latin.TextRecognizerOptions;

import org.json.JSONArray;
import org.json.JSONObject;

import java.nio.ByteBuffer;

// On-device text recognition for camera frames coming from Unity. Nothing leaves the headset.
public class ReadLensOcr {
    public interface Callback {
        void onResult(String json);
        void onError(String message);
    }

    private static TextRecognizer recognizer;

    public static void recognize(byte[] rgba, int width, int height, Callback callback) {
        try {
            if (recognizer == null) recognizer = TextRecognition.getClient(TextRecognizerOptions.DEFAULT_OPTIONS);
            Bitmap raw = Bitmap.createBitmap(width, height, Bitmap.Config.ARGB_8888);
            raw.copyPixelsFromBuffer(ByteBuffer.wrap(rgba));
            // Unity pixel rows start at the bottom of the image.
            Matrix flip = new Matrix();
            flip.preScale(1, -1);
            Bitmap upright = Bitmap.createBitmap(raw, 0, 0, width, height, flip, false);
            recognizer.process(InputImage.fromBitmap(upright, 0))
                    .addOnSuccessListener(text -> {
                        try {
                            callback.onResult(toJson(text, width, height));
                        } catch (Exception e) {
                            callback.onError(e.toString());
                        }
                    })
                    .addOnFailureListener(e -> callback.onError(e.toString()));
        } catch (Throwable e) {
            callback.onError(e.toString());
        }
    }

    private static String toJson(Text text, int width, int height) throws Exception {
        JSONArray blocks = new JSONArray();
        for (Text.TextBlock block : text.getTextBlocks()) {
            JSONArray lines = new JSONArray();
            for (Text.Line line : block.getLines()) {
                lines.put(new JSONObject().put("text", line.getText()).put("box", box(line.getBoundingBox())));
            }
            blocks.put(new JSONObject().put("text", block.getText()).put("box", box(block.getBoundingBox())).put("lines", lines));
        }
        return new JSONObject().put("width", width).put("height", height).put("blocks", blocks).toString();
    }

    private static JSONArray box(Rect r) {
        JSONArray a = new JSONArray();
        if (r == null) return a;
        return a.put(r.left).put(r.top).put(r.right).put(r.bottom);
    }
}
