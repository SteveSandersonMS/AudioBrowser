export async function showDirectoryPicker() {
    const dir = await window.showDirectoryPicker();

    // Track the dir in history.state
    const state = history.state || {};
    state.currentDir = dir;
    history.replaceState(state, '');

    return {
        name: dir.name,
        instance: DotNet.createJSObjectReference(dir)
    };
}

export async function reopenLastDirectory() {
    const value = history.state && history.state.currentDir;
    return value ? { name: value.name, instance: DotNet.createJSObjectReference(value) } : null;
}

export async function getFiles(directory) {
    // Build an array containing all the file entries
    const result = [];
    for await (const entry of directory.values())
        result.push(await entry.getFile());

    // For each entry, get name/size/modified
    return result.map(r => ({ name: r.name, size: r.size, lastModified: r.lastModifiedDate.toISOString() }));
}

export async function decodeAudioFile(name) {
    // Read the file
    const dir = history.state.currentDir;
    const fileHandle = await dir.getFileHandle(name);
    const file = await fileHandle.getFile();
    const fileBytes = await file.arrayBuffer();

    // Decode and extract the audio samples
    const audioBuffer = await new OfflineAudioContext(2, 44100, 44100).decodeAudioData(fileBytes);
    return new Uint8Array(audioBuffer.getChannelData(0).buffer);
}

export async function playAudioFile(name) {
    const samples = await decodeAudioFile(name);
    return playAudioData(samples);
}

export async function playAudioData(samples) {
    // Populate an AudioBuffer object
    const floatData = new Float32Array(samples.buffer);
    const audioContext = new AudioContext();
    const buffer = audioContext.createBuffer(/*numOfChannels*/ 1, floatData.length, /*sampleRate*/ 48000);
    buffer.copyToChannel(floatData, 0);

    // Start playing it
    const source = audioContext.createBufferSource();
    source.buffer = buffer;
    source.connect(audioContext.destination);
    source.start();
    return source;
}
